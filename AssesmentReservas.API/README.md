# Assesment Reservas — Plataforma de Rentas Cortas

Plataforma unificada para renta corta de inmuebles: catálogo público, wishlist,
reservas sin solapamiento, validación de identidad (KYC) con OCR, notificaciones
omnicanal, dashboard para propietarios y exportación de reportes en Excel.

Incluye frontend en **Razor/MVC** (sobre el diseño de Stitch) y una **API REST**
(`/api/...`) consumible aparte.

### Descripción (semi-técnica)

Aplicación **ASP.NET Core (.NET 10)** con arquitectura **MVC + capa de servicios**.
El dominio (inmuebles, reservas, favoritos, KYC, notificaciones) vive en EF Core
sobre **PostgreSQL**. Cada caso de uso se resuelve en un *service* desacoplado por
interfaces, consumido tanto por los **controladores MVC** (las vistas Razor que ve
el usuario) como por los **controladores de API** (`/api/...`, para integraciones).

Puntos técnicos destacables:

- **Disponibilidad estricta:** la creación de reservas valida solapamientos dentro
  de una **transacción `Serializable`** (evita *double-booking* ante concurrencia).
- **Horarios estándar:** check-in 14:00 / check-out 12:00 centralizados en una
  política única (`BookingPolicy`).
- **KYC con IA/OCR:** la cédula se procesa con **Tesseract**, se extraen los datos y
  se emite un veredicto; el documento se **cifra (AES-GCM)** y se **elimina** tras la
  validación. Es requisito para la primera reserva.
- **Infraestructura desacoplada:** **Redis** (cache del catálogo), **MinIO/S3**
  (imágenes y documentos), **MailKit + MailHog** (correos), **Serilog** (logs),
  **ClosedXML** (reportes Excel) y un **BackgroundService** de recordatorios.
- **Seguridad:** autenticación por **cookies + ASP.NET Identity**, autorización por
  rol y *anti-forgery* en formularios.

---

## 1. Requisitos previos

- [Docker](https://docs.docker.com/get-docker/) y Docker Compose (v2).
- Solo para desarrollo local fuera de Docker: [.NET 10 SDK](https://dotnet.microsoft.com/download).

No se requiere instalar PostgreSQL, Redis, MinIO ni un servidor SMTP: todo se
levanta vía `docker compose`.

### Opcional — KYC/OCR en desarrollo local

El OCR (Tesseract) ya viene incluido en la imagen Docker. Para que el **KYC
funcione corriendo la API en local** (Modo B), instala Tesseract + idiomas en el
host (ejemplo Ubuntu/Debian):

```bash
sudo apt-get update
sudo apt-get install -y tesseract-ocr tesseract-ocr-spa tesseract-ocr-eng \
  libtesseract-dev libleptonica-dev

# El wrapper .NET busca libtesseract.so / libleptonica.so:
ARCH=$(uname -m)-linux-gnu
sudo ln -sf /usr/lib/$ARCH/libtesseract.so.5 /usr/lib/$ARCH/libtesseract.so 2>/dev/null
LEPT=$(ls /usr/lib/$ARCH/liblept*.so.* 2>/dev/null | head -1)
[ -n "$LEPT" ] && sudo ln -sf "$LEPT" /usr/lib/$ARCH/libleptonica.so
sudo ldconfig
```

La ruta de datos (`Kyc:TessDataPath`) es `/usr/share/tesseract-ocr/5/tessdata`,
**la misma en local y en Docker**, por lo que no hay que cambiar configuración.

---

## 2. Levantar el proyecto

Hay dos formas de ejecutarlo (elige una a la vez para no correr la API duplicada).

### Modo A: Todo en Docker (recomendado para evaluar)

Desde la raíz del repositorio:

```bash
docker compose up --build
```

Esto construye la API e inicia todos los servicios. La aplicación aplica las
migraciones de EF Core y siembra los roles (`Guest`, `Owner`) automáticamente al
arrancar.

| Servicio        | URL / Puerto                  | Notas                                  |
|-----------------|-------------------------------|----------------------------------------|
| API             | http://localhost:8080         | Aplicación principal (.NET 10)         |
| PostgreSQL      | localhost:5432                | usuario/clave/db: `reservas`           |
| Redis           | localhost:6379                | Cache distribuido                      |
| MinIO (S3 API)  | http://localhost:9000         | `minioadmin` / `minioadmin`            |
| MinIO (consola) | http://localhost:9001         | UI web de objetos                      |
| MailHog (SMTP)  | localhost:1025                | Captura de correos                     |
| MailHog (UI)    | http://localhost:8025         | Ver correos enviados por la app        |

Para detener y limpiar:

```bash
docker compose down            # detiene
docker compose down -v         # detiene y borra volúmenes (datos)
```

### Modo B: Desarrollo local (API en el IDE + infraestructura en Docker)

Pensado para depurar la API desde el IDE mientras Postgres/Redis/MinIO/MailHog
corren en contenedores. La configuración de `Development` ya apunta a `localhost`
(ver `appsettings.Development.json`), por lo que **no hay que cambiar nada**.

1. Levanta **solo la infraestructura** (sin el contenedor de la API, para no
   ejecutar la API dos veces):

   ```bash
   docker compose up -d postgres redis minio mailhog
   ```

2. Ejecuta la API desde el IDE (perfil *http*) o por consola:

   ```bash
   cd AssesmentReservas.API
   dotnet run --launch-profile http
   ```

   La API queda en **http://localhost:5152** y aplica migraciones/seed al arrancar.

> **¿Por qué funciona en ambos modos?** En Docker, `docker-compose.yml` define las
> cadenas de conexión por **variables de entorno** (`postgres`, `redis`, …), que
> tienen prioridad sobre los `appsettings.*.json`. En local, al usar el entorno
> `Development`, se cargan los valores de `localhost` de `appsettings.Development.json`.
>
> **KYC en local:** requiere instalar Tesseract en el host (ver
> *Opcional — KYC/OCR en desarrollo local* en la sección 1). Sin él, un KYC
> devolverá "Rechazado". En Docker ya viene incluido.

---

## 3. Usuarios y acceso por rol

El sistema maneja **dos roles** (no existe un rol "admin"); el rol de mayor
privilegio es **Propietario**, que administra inventario y métricas:

| Rol | Quién es | Qué puede hacer |
|-----|----------|-----------------|
| **Guest** (Huésped) | Arrendatario | Explorar, favoritos, validar KYC, reservar, ver sus reservas y notificaciones |
| **Owner** (Propietario) | Anfitrión | Todo lo anterior + publicar/editar inmuebles, subir fotos, dashboard y reportes Excel |

> **No hay usuarios precargados**: cada persona crea su cuenta y elige su rol al
> registrarse. El registro inicia sesión automáticamente (cookie de Identity).

### Cómo acceder a cada rol

1. Abre la app (Docker → http://localhost:8080, local → http://localhost:5152).
2. Menú **Ingresar → Regístrate** (`/Account/Register`).
3. En **"Quiero registrarme como"** elige:
   - **Huésped** para el flujo de arrendatario, o
   - **Propietario** para gestionar inmuebles y ver el panel.
4. Para volver a entrar luego: **Ingresar** (`/Account/Login`) con tu correo y clave.

Reglas de contraseña: mínimo 8 caracteres.

### Flujo de prueba sugerido

- **Como Propietario:** regístrate como *Propietario* → **Panel** (`/Owner`) →
  *Inmuebles* → **Publicar** → sube una foto → revisa **Dashboard** y descarga el
  **Excel**.
- **Como Huésped:** regístrate como *Huésped* → entra a un inmueble del catálogo →
  *Reservar* te pedirá **KYC** (`/Kyc`, sube foto de cédula) → luego confirma la
  reserva → revisa **Mis reservas**, **Favoritos**, **Notificaciones** y el correo
  en **MailHog** (http://localhost:8025).

> Tip: para reservar necesitas el **KYC aprobado**. En Docker el OCR ya funciona;
> en local requiere instalar Tesseract (sección 1).

---

## 4. Arquitectura y decisiones técnicas

### 4.1 Stack

| Capa             | Tecnología                                             |
|------------------|--------------------------------------------------------|
| Core             | ASP.NET Core MVC + API — **.NET 10**                   |
| ORM / BD         | Entity Framework Core + **PostgreSQL** (Npgsql)        |
| Autenticación    | ASP.NET Identity + **Cookies**                         |
| Almacenamiento   | **MinIO** (S3) vía AWS SDK — imágenes y documentos     |
| Cache            | **Redis** (cache distribuido)                          |
| Correos          | **MailKit** (SMTP, MailHog en dev)                     |
| Reportes         | **ClosedXML** (.xlsx)                                   |
| Logging          | **Serilog** (consola + archivo)                        |
| IA / KYC         | **Tesseract OCR** (local, español + inglés)            |
| Infraestructura  | **Docker / Docker Compose**                            |

### 4.2 Organización del código (MVC + servicios)

```
AssesmentReservas.API/
├── Models/          # Entidades de dominio (Property, Booking, Favorite, Kyc...)
├── Enums/           # Estados y constantes tipadas (BookingStatus, Roles...)
├── Data/            # AppDbContext (IdentityDbContext) + migraciones
├── DTOs/            # Contratos de entrada/salida por módulo
├── Interfaces/      # Abstracciones de servicios
├── Services/        # Lógica de negocio por dominio
├── Controllers/     # Endpoints MVC/API
├── Settings/        # Tipados de configuración (Minio, Mail, Kyc)
└── Views/           # Vistas MVC
```

### 4.3 Cómo se abordan los problemas técnicos de la prueba

- **Anti double-booking (disponibilidad estricta):** las fechas se modelan con
  `DateOnly` y existe un índice `(PropertyId, CheckInDate, CheckOutDate)`. La
  validación de solapamiento se hace en el servicio de reservas con la condición
  `existing.CheckIn < new.CheckOut && new.CheckIn < existing.CheckOut`. *(Mejora
  planeada: exclusion constraint nativa de PostgreSQL con `daterange` + `gist`.)*

- **Horarios estándar:** centralizados en `BookingPolicy` (Check-in 14:00,
  Check-out 12:00), evitando literales dispersos.

- **Autenticación diferida:** navegación y catálogo son anónimos; Identity solo
  se exige al reservar, pagar o guardar favoritos permanentes.

- **KYC con IA:** la imagen de la cédula se procesa con Tesseract OCR para
  extraer Nombres, Apellidos, Documento y Fecha de Nacimiento, y se emite un
  veredicto. El documento se **cifra** (AES) antes de subirse a un bucket privado
  de MinIO y se **elimina de forma segura** tras el procesamiento.

- **Notificaciones omnicanal:** in-app (persistidas en BD) + correo (MailKit),
  desacopladas detrás de una interfaz de notificaciones.

- **Reportes:** ClosedXML genera `.xlsx` para el portafolio completo o por
  inmueble, con fechas, precio, datos del huésped e inmueble.

- **Configuración por entorno:** todo parametrizable vía variables de entorno
  (sobrescriben `appsettings.json`), como exige el flujo de Docker.

---

## 5. Consumo de la API

Los ejemplos de request/response por módulo se documentan en
[`AssesmentReservas.API/reservas.http`](AssesmentReservas.API/reservas.http)
(ejecutable desde VS Code / Rider) y se irán ampliando conforme se construye
cada módulo.

##6. GitHub
https://github.com/Alexis-pr/AssesmentReservas/tree/main
