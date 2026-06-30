# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AssesmentReservas.API.csproj ./
RUN dotnet restore AssesmentReservas.API.csproj
COPY . .
RUN dotnet publish AssesmentReservas.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Dependencias nativas para Tesseract OCR (KYC) + datos entrenados (español e inglés).
# libwebp-dev → leptonica con soporte WebP (formato común de cámaras móviles).
RUN apt-get update && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-spa \
    tesseract-ocr-eng \
    libwebp-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AssesmentReservas.API.dll"]
