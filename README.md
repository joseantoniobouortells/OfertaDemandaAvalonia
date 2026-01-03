# OfertaDemanda

OfertaDemanda es un simulador visual de microeconomía que replica y amplía el comportamiento del archivo de referencia `reference/oferta-demanda.html`. La aplicación permite experimentar con curvas de demanda y oferta, analizar costes de una empresa competitiva, estudiar la decisión óptima del monopolista y medir la elasticidad-precio usando expresiones matemáticas libres. Está pensada para docencia: cada pestaña recalcula de forma inmediata las curvas y métricas relevantes y muestra las áreas de excedentes y pérdidas de eficiencia mediante gráficos LiveCharts.

## Propósito y funcionamiento

1. **Mercado**: acepta expresiones de demanda y oferta inversa (por ejemplo `100 - 0.5q`) y parámetros de shocks/impuestos. Calcula el equilibrio con y sin impuesto, excedentes, recaudación y pérdida irrecuperable; dibuja las curvas base y desplazadas con sus áreas.
2. **Empresa (CP/LP)**: recibe la función de coste total y un precio de mercado o modo de largo plazo. Deriva CMg, CMe y CMeV, encuentra la cantidad óptima según el modo y calcula beneficios.
3. **Monopolio**: evalúa demanda inversa y costes totales para construir ingresos/costes marginales, encontrar el punto donde IMg=CMg, comparar con competencia perfecta y cuantificar la pérdida de eficiencia.
4. **Elasticidad**: usa la misma demanda del Mercado y un precio objetivo para estimar elasticidad-precio puntual mediante derivada numérica y marca el punto en la curva.
5. **Isobeneficio**: muestra curvas de isobeneficio del mercado (Π̄ negativos/cero/positivos) frente a la demanda y, en una pestaña separada, las curvas de cada empresa con su intersección p=P*, el valor óptimo q_i* (CMg=P*) y el diagnóstico “Gana/Pierde/Cero”. Incluye editor CRUD de empresas, persistencia y panel de fórmulas en texto plano que explica las ecuaciones usadas.

Cada pestaña comparte el motor del proyecto `OfertaDemanda.Core`, por lo que los cambios en las expresiones o parámetros se traducen en nuevas evaluaciones sin reescribir lógica en la UI.

## Arquitectura y módulos

- **`src/OfertaDemanda.Core`**: biblioteca netstandard con tres piezas principales:
  - Parser seguro de expresiones (`Expressions/`) que convierte cadenas con `q`, potencias y multiplicación implícita a RPN evaluable.
  - Utilidades numéricas (`Numerics/`) con integración, derivación y búsqueda de raíces robusta (`FindRoot`, `Integrate`, `Derivative`, `Safe`).
  - Modelos económicos (`Models/`) que encapsulan parámetros/resultados y exponen calculadoras puras (`MarketCalculator`, `FirmCalculator`, `MonopolyCalculator`, `ElasticityCalculator`).
- **`src/OfertaDemanda.Desktop`**: cliente Avalonia (.NET 8) con MVVM Toolkit + LiveCharts2. Los view models contienen el estado observable, parsean las entradas del usuario mediante el motor y transforman los resultados en series de gráficos y métricas de texto.
- **`test/OfertaDemanda.Core.Tests`**: batería xUnit que valida parser, métodos numéricos y modelos con el conjunto de expresiones del HTML original.
- **`scripts/publish-macos.sh`**: automatiza `dotnet publish` para generar un bundle `.app` autocontenido y opcionalmente instalarlo en `/Applications`.
- **`scripts/create-windows-msix.ps1`**: genera un `.msix` firmado para Windows 11 usando el SDK de Windows.
- **`scripts/create-windows-msi.ps1`**: genera un `.msi` tradicional con WiX Toolset v4.

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

## Publicar Windows desde macOS (Docker)

Requisitos:
- Docker Desktop con contenedores Linux.

Ejecutar:

```bash
./scripts/docker-publish-windows.sh
```

Salida:
- `artifacts/publish/win-x64` (contiene el `.exe` autocontenido).

Nota: este paso solo genera el publish win-x64; el empaquetado MSI/MSIX sigue requiriendo tooling de Windows.

## Releases automáticas (GitHub Actions)

El repositorio incluye un flujo que crea una release y adjunta artefactos para Windows (MSI/MSIX), macOS (DMG) y Linux (publish empaquetado).

Opciones para lanzarlo:

```bash
# Opción 1: tag (se dispara automáticamente)
git tag v1.1.2
git push origin v1.1.2

# Opción 2: manual (workflow_dispatch)
GH_CONFIG_DIR=~/.config/gh-personal gh workflow run release.yml -f tag=v1.1.2
```

Salida esperada:
- Windows: `.msi` y `.msix` en Assets de la release.
- macOS: `.dmg` en Assets.
- Linux: `OfertaDemanda.Desktop-linux-x64.tar.gz` en Assets.

## Cómo instalar

### macOS (DMG)
- Descarga el `.dmg`, ábrelo y arrastra la app a `Applications`.
- Si Gatekeeper bloquea la primera ejecución, usa clic derecho → “Abrir” o elimina la cuarentena:
  ```bash
  xattr -dr com.apple.quarantine /Applications/OfertaDemanda.app
  ```

### Windows (MSIX)
- **Firmado**: doble clic e instala.
- **Sin firmar / auto-firmado**: instala el certificado y luego el MSIX:
  ```powershell
  Import-Certificate -FilePath ".\OfertaDemandaAvalonia.cer" -CertStoreLocation "Cert:\CurrentUser\TrustedPeople"
  Add-AppxPackage -Path ".\OfertaDemandaAvalonia.msix"
  ```

### Windows (MSI)
- Si el MSI incluye `cab1.cab`, asegúrate de tener **ambos** en la misma carpeta.
- Alternativa recomendada: usa el ZIP `*_msi.zip` de la release, que ya contiene todo.

## MSIX en Windows 11

```powershell
# Crear MSIX (Release, win-x64)
powershell -ExecutionPolicy Bypass -File .\scripts\create-windows-msix.ps1

# Instalar localmente el MSIX generado
Add-AppxPackage -Path .\artifacts\msix\OfertaDemandaAvalonia_<version>_win-x64.msix
```

Si no defines `SIGN_CERT_PFX`, el script genera un certificado de desarrollo y lo deja en `artifacts\msix\OfertaDemandaAvalonia.Dev.cer`. Para confiar localmente:

```powershell
Import-Certificate -FilePath .\artifacts\msix\OfertaDemandaAvalonia.Dev.cer -CertStoreLocation Cert:\CurrentUser\TrustedPeople
Import-Certificate -FilePath .\artifacts\msix\OfertaDemandaAvalonia.Dev.cer -CertStoreLocation Cert:\CurrentUser\Root
```

Si tienes un PFX propio, exporta estas variables antes de ejecutar:

```powershell
$env:SIGN_CERT_PFX="C:\ruta\cert.pfx"
$env:SIGN_CERT_PASSWORD="tu-password"
```

## MSI en Windows 11

```powershell
# Crear MSI (Release, win-x64)
powershell -ExecutionPolicy Bypass -File .\scripts\create-windows-msi.ps1
```

El script usa WiX Toolset v4 (dotnet tool) y lo instala si no esta disponible.

Salida esperada:

- `artifacts\msi\OfertaDemandaAvalonia_<version>_win-x64.msi`

## Ajustes de tema y preferencias

- La pestaña **Configuración** expone selectores de **tema** (Sistema/Claro/Oscuro) y **idioma** (ES/EN/FR/IT) que aplican cambios en caliente.
- La elección persiste en `ApplicationData/OfertaDemandaAvalonia/settings.json` (por ejemplo: `~/Library/Application Support/OfertaDemandaAvalonia/settings.json` en macOS o `%APPDATA%\OfertaDemandaAvalonia\settings.json` en Windows) y el mismo archivo guarda la lista de empresas, parámetros del tab Isobeneficio y el idioma seleccionado.
- Para restablecer la apariencia basta con borrar ese archivo y reiniciar la app; se volverá al modo “Predeterminado del sistema”.
- El almacenamiento es per‑usuario y no depende de servicios externos.

## Idiomas / Languages

- Las cadenas de UI viven en `src/OfertaDemanda.Desktop/Resources/Strings.resx` (español por defecto) y en los satélites `Strings.en.resx`, `Strings.fr.resx`, `Strings.it.resx`.
- Para añadir un texto nuevo: define la clave en todos los `.resx` y consúmela desde XAML (`Localization[Clave]`) o desde los view models mediante `LocalizationService`.
- Para añadir un idioma nuevo: agrega `Strings.<culture>.resx`, registra el culture en `src/OfertaDemanda.Desktop/Services/LocalizationService.cs` y añade la opción en `src/OfertaDemanda.Desktop/ViewModels/SettingsViewModel.cs`.

## Flujo de trabajo recomendado

1. Modela los cambios matemáticos en `Core` (expresión parser, cálculos, etc.) y cubre el escenario con pruebas en `test/OfertaDemanda.Core.Tests`.
2. Consume los nuevos datos desde los view models correspondientes y refleja los valores en gráficos/etiquetas. Mantén la separación UI ↔ motor: la UI nunca debe calcular integrales ni derivadas directamente.
3. Valida manualmente la pestaña afectada ejecutando `dotnet run` y comparando la gráfica con el HTML de referencia cuando aplique.
4. Para liberaciones o demostraciones, usa el script de publicación para obtener un `.app` firmado localmente.

Este README recoge el contexto necesario para retomar el desarrollo rápidamente: describe qué calcula cada módulo, cómo se conectan, dónde viven los archivos relevantes y qué comandos ejecutar para compilar, probar o distribuir la aplicación.
