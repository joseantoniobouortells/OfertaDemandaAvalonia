# Repository Guidelines

## Project Structure & Module Organization
La solución principal (`OfertaDemanda.sln`) orquesta los dos proyectos de `src`: `OfertaDemanda.Core` contiene el parser de expresiones, los métodos numéricos y los modelos económicos, mientras `OfertaDemanda.Desktop` aloja la UI Avalonia (vistas Mercado/Empresa/Monopolio/Elasticidad). Las pruebas unitarias viven en `test/OfertaDemanda.Core.Tests` y cubren el motor matemático con xUnit. Los recursos de referencia (HTML original y activos adicionales) residen en `reference/`, y los bundles publicados se generan dentro de `artifacts/`. Los scripts reutilizables (como la publicación macOS) están en `scripts/`.

## Build, Test, and Development Commands
- `dotnet restore` — restaura paquetes NuGet antes de cualquier build o CI.
- `dotnet build OfertaDemanda.sln -c Debug` — compila toda la solución y valida que no existan warnings críticos.
- `dotnet test test/OfertaDemanda.Core.Tests/OfertaDemanda.Core.Tests.csproj` — ejecuta las pruebas xUnit y debe pasar antes de subir cambios.
- `dotnet run --project src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj` — lanza la aplicación Avalonia para validar la UI.
- `./scripts/publish-macos.sh --project src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj --config Release --install` — crea e instala un bundle `.app` para validaciones manuales en macOS.

## Coding Style & Naming Conventions
Usa C# 12 con namespaces de archivo, `global using` mínimos y sangría de 4 espacios. Sigue `PascalCase` para tipos públicos y métodos (`MarketCalculator`, `FindRoot`) y `camelCase` para parámetros, variables locales y funciones lambda. Prefiere `record`/`struct` inmutables para paquetes de datos y `readonly` siempre que sea posible. Mantén los strings mostrados al usuario en español neutro para alinear la UI con el HTML original. Separa lógica de dominio en `Core` y deja que la UI interactúe solo con view models u objetos calculados; evita mezclar código Avalonia en el motor.

## Testing Guidelines
Amplía `test/OfertaDemanda.Core.Tests` con clases nuevas por área (`ExpressionTests`, `NumericMethodsTests`, etc.). Nombra los métodos describiendo la expectativa (por ejemplo, `MonopolyReferenceProducesPositiveProfit`). Usa `[Theory]` + `[InlineData]` para cubrir expresiones parametrizadas y `Assert.InRange` para tolerancias numéricas, replicando los valores del HTML de referencia. Ejecuta `dotnet test` antes de abrir un PR y adjunta casos nuevos que fallen antes de tu corrección. Los cambios en modelos deben venir acompañados de pruebas que ejerzan el escenario económico que tocas.

## Commit & Pull Request Guidelines
Sigue el estilo visto en `git log`: mensajes cortos en imperativo (habitualmente en español) que expliquen la intención, p. ej. `Ajustar elasticidad por tramos`. Una PR debe incluir: resumen funcional, pasos de prueba manual (incluyendo comandos ejecutados), capturas de la UI cuando cambie el layout o los gráficos, y referencias a issues o discusiones. Confirma que la solución compila, que las pruebas pasan y que, si aplicaste `scripts/publish-macos.sh`, adjuntes la salida relevante para quien revise.
