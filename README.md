# OfertaDemanda

OfertaDemanda es un simulador visual de microeconomía que replica y amplía el comportamiento del archivo de referencia `reference/oferta-demanda.html`. La aplicación permite experimentar con curvas de demanda y oferta, analizar costes de una empresa competitiva, estudiar la decisión óptima del monopolista y medir la elasticidad-precio usando expresiones matemáticas libres. Está pensada para docencia: cada pestaña recalcula de forma inmediata las curvas y métricas relevantes y muestra las áreas de excedentes y pérdidas de eficiencia mediante gráficos LiveCharts.

## Propósito y funcionamiento

1. **Mercado**: acepta expresiones de demanda y oferta inversa (por ejemplo `100 - 0.5q`) y parámetros de shocks/impuestos. Calcula el equilibrio con y sin impuesto, excedentes, recaudación y pérdida irrecuperable; dibuja las curvas base y desplazadas con sus áreas.
2. **Empresa (CP/LP)**: recibe la función de coste total y un precio de mercado o modo de largo plazo. Deriva CMg, CMe y CMeV, encuentra la cantidad óptima según el modo y calcula beneficios.
3. **Monopolio**: evalúa demanda inversa y costes totales para construir ingresos/costes marginales, encontrar el punto donde IMg=CMg, comparar con competencia perfecta y cuantificar la pérdida de eficiencia.
4. **Elasticidad**: usa la misma demanda del Mercado y un precio objetivo para estimar elasticidad-precio puntual mediante derivada numérica y marca el punto en la curva.

Cada pestaña comparte el motor del proyecto `OfertaDemanda.Core`, por lo que los cambios en las expresiones o parámetros se traducen en nuevas evaluaciones sin reescribir lógica en la UI.

## Arquitectura y módulos

- **`src/OfertaDemanda.Core`**: biblioteca netstandard con tres piezas principales:
  - Parser seguro de expresiones (`Expressions/`) que convierte cadenas con `q`, potencias y multiplicación implícita a RPN evaluable.
  - Utilidades numéricas (`Numerics/`) con integración, derivación y búsqueda de raíces robusta (`FindRoot`, `Integrate`, `Derivative`, `Safe`).
  - Modelos económicos (`Models/`) que encapsulan parámetros/resultados y exponen calculadoras puras (`MarketCalculator`, `FirmCalculator`, `MonopolyCalculator`, `ElasticityCalculator`).
- **`src/OfertaDemanda.Desktop`**: cliente Avalonia (.NET 8) con MVVM Toolkit + LiveCharts2. Los view models contienen el estado observable, parsean las entradas del usuario mediante el motor y transforman los resultados en series de gráficos y métricas de texto.
- **`test/OfertaDemanda.Core.Tests`**: batería xUnit que valida parser, métodos numéricos y modelos con el conjunto de expresiones del HTML original.
- **`scripts/publish-macos.sh`**: automatiza `dotnet publish` para generar un bundle `.app` autocontenido y opcionalmente instalarlo en `/Applications`.

### Estructura de carpetas

| Ruta                               | Descripción                                                                 |
|------------------------------------|-----------------------------------------------------------------------------|
| `reference/`                       | Material legado (HTML, assets) usado como especificación funcional.        |
| `src/OfertaDemanda.Core`           | Motor matemático y contratos compartidos.                                  |
| `src/OfertaDemanda.Desktop`        | UI Avalonia + view models + composición principal.                         |
| `test/OfertaDemanda.Core.Tests`    | Pruebas automatizadas de Core con xUnit.                                   |
| `scripts/`                         | Scripts auxiliares de build/publicación.                                   |
| `artifacts/`                       | Salida de builds/publish (carpeta ignorada por Git).                       |

## Requisitos y configuración

- .NET 8 SDK (incluye la plantilla de Avalonia referenciada en el `.csproj`).
- macOS/Windows/Linux con soporte para Skia (LiveCharts2 lo usa para dibujar).
- No se requieren secretos ni servicios externos: todo el cálculo es local.

## Comandos habituales

```bash
# Restaurar dependencias de toda la solución
dotnet restore

# Compilar en Debug y verificar warnings
dotnet build OfertaDemanda.sln -c Debug

# Ejecutar pruebas de Core
dotnet test test/OfertaDemanda.Core.Tests/OfertaDemanda.Core.Tests.csproj

# Lanzar la app de escritorio (hot reload disponible desde IDEs)
dotnet run --project src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj

# Generar bundle macOS autocontenido (Release + install opcional)
./scripts/publish-macos.sh --project src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj --config Release --install
```

## Flujo de trabajo recomendado

1. Modela los cambios matemáticos en `Core` (expresión parser, cálculos, etc.) y cubre el escenario con pruebas en `test/OfertaDemanda.Core.Tests`.
2. Consume los nuevos datos desde los view models correspondientes y refleja los valores en gráficos/etiquetas. Mantén la separación UI ↔ motor: la UI nunca debe calcular integrales ni derivadas directamente.
3. Valida manualmente la pestaña afectada ejecutando `dotnet run` y comparando la gráfica con el HTML de referencia cuando aplique.
4. Para liberaciones o demostraciones, usa el script de publicación para obtener un `.app` firmado localmente.

Este README recoge el contexto necesario para retomar el desarrollo rápidamente: describe qué calcula cada módulo, cómo se conectan, dónde viven los archivos relevantes y qué comandos ejecutar para compilar, probar o distribuir la aplicación.***
