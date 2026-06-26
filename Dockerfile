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
RUN apt-get update && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-spa \
    tesseract-ocr-eng \
    libleptonica-dev \
    libtesseract-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Symlinks con los nombres exactos que busca el binding Tesseract .NET (InteropDotNet).
# Se crean DESPUÉS del COPY para que queden en /app, que es el primer dir que busca.
RUN ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/libleptonica-1.82.0.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/libtesseract50.so

ENV LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AssesmentReservas.API.dll"]
