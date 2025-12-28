## OfertaDemanda

Port funcional del simulador `reference/oferta-demanda.html` a una solución .NET 8 multiplataforma (Avalonia + LiveCharts2 + MVVM). El repositorio incluye:

- `OfertaDemanda.sln`
- `src/OfertaDemanda.Core`: motor numérico y parser seguro de expresiones.
- `src/OfertaDemanda.Desktop`: UI Avalonia con pestañas Mercado/Empresa/Monopolio/Elasticidad.
- `test/OfertaDemanda.Core.Tests`: pruebas xUnit para parser, utilidades numéricas y modelos base.

### Requisitos

Necesitas .NET 8 SDK y el template de Avalonia (ya referenciado en el proyecto).

### Comandos habituales

```bash
# Restaurar dependencias
dotnet restore

# Compilar toda la solución
dotnet build

# Ejecutar las pruebas del core
dotnet test

# Lanzar la app de escritorio
dotnet run --project src/OfertaDemanda.Desktop
```

La ventana se abre con la pestaña de Mercado por defecto y replica los parámetros y rangos del HTML original.
