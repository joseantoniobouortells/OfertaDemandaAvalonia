Objetivo
Portar la funcionalidad de reference/oferta-demanda.html a una app de escritorio multiplataforma (.NET 8 + Avalonia 11) con MVVM. El HTML es la referencia funcional. Genera el proyecto completo (solución + proyectos + tests) con un MVP equivalente.

Plataformas
- Debe ejecutarse en macOS (principal), Windows y Linux.
- No usar WebView ni Plotly. Gráficos nativos con LiveCharts2 (SkiaSharp) para Avalonia.

Dependencias
- UI: Avalonia
- Charts: LiveCharts2 para Avalonia (paquete NuGet: LiveChartsCore.SkiaSharpView.Avalonia)
- Tests: xUnit

Estructura del repo (crear exactamente)
- OfertaDemanda.sln en la raíz
- reference/oferta-demanda.html (conservar como referencia)
- src/OfertaDemanda.Desktop  (Avalonia app)
- src/OfertaDemanda.Core     (motor: parser expresiones, raíces, derivadas, integrales, modelos)
- test/OfertaDemanda.Core.Tests (xUnit)

Requisitos de fidelidad (defaults y rangos como en el HTML)
A) Defaults (valores iniciales)
- Tab inicial: "mercado"
- Modo empresa inicial: "CP"
- Mercado:
  - Demanda inversa (texto): "100 - 0.5q"
  - Oferta inversa (texto): "20 + 0.5q"
  - Shock demanda ΔD: 0
  - Shock oferta ΔS: 0
  - Impuesto unitario t: 0
- Empresa (competencia perfecta):
  - CT(q) (texto): "200 + 10q + 0.5q^2"
  - Precio P (slider): 40
- Monopolio:
  - Demanda inversa Pd(q) (texto): "120 - q"
  - Coste total CT(q) (texto): "100 + 10q + 0.2q^2"
- Elasticidad:
  - Usar la demanda base del mercado (demandBase) y el shock ΔD del mercado (shockD) para Pd(q)
  - Precio para elasticidad (slider): 50

B) Rangos de controles (sliders)
- Shock Demanda ΔD: min -30, max 30
- Shock Oferta ΔS: min -30, max 30
- Impuesto t: min 0, max 30
- Precio P (empresa CP/LP): min 10, max 100
- Precio P (elasticidad): min 10, max 110

C) Discretización de curvas para gráficos (MVP, sin sombreado obligatorio)
- Mercado: muestrear Q con 100 puntos usando Q = i*2 (i=0..99) => Q en [0,198]. Mostrar ejes con:
  - xaxis título "Q" y rango visible [0,100]
  - yaxis título "P" y rango visible [0,150]
- Empresa: muestrear q con 100 puntos usando q = i*0.6 (i=0..99) => q en [0,59.4]. yaxis título "Costes / P" rango [0,120]
- Monopolio: muestrear q con 100 puntos usando q = i (i=0..99)
- Elasticidad: muestrear q con 150 puntos usando q = i (i=0..149)

Restricciones de implementación (seguridad y compatibilidad con expresiones del HTML)
- NO usar eval dinámico inseguro.
- Implementar un parser/evaluador seguro para expresiones con variable q:
  - Soportar: números decimales, variable q, + - * /, paréntesis, potencia ^.
  - MUY IMPORTANTE: soportar multiplicación implícita como en el HTML:
    - "0.5q" => 0.5 * q
    - "10q"  => 10 * q
    - "q^2"  => q ^ 2
    - "(q+1)(q-1)" => (q+1) * (q-1) (ideal)
  - Manejo de errores: devolver error explicable (posición/token) y la UI debe mostrarlo sin crashear.

Core (OfertaDemanda.Core) - implementar
1) Expression
- Tokenizer + parser (Shunting-yard o AST) + evaluator (double).
- Normalización opcional: convertir implícitas a explícitas durante tokenización (p.ej. Number seguido de Identifier, ')' seguido de '(', etc.).

2) Numérica
- Root finding: bisección con fallback por muestreo si no hay cambio de signo.
  - Firma equivalente al HTML: FindRoot(f, low=0, high=1000) (por defecto).
  - Rutas específicas:
    - Mercado: FindRoot(f) usando low=0, high=1000.
    - Empresa CP: FindRoot((q)=>CMg(q)-P, 0, 300)
    - Empresa LP: FindRoot((q)=>CMg(q)-CMe(q), 0.1, 500)
    - Monopolio qm: FindRoot((q)=>IMg(q)-CMg(q), 0, 300)
    - Monopolio q_CP: FindRoot((q)=>Pd(q)-CMg(q), 0, 300)
    - Elasticidad: FindRoot((q)=>Pd(q)-P, 0, 500)
- Derivada numérica central.
- Integración por trapecios.
- “Safe/Clamp”: evitar NaN/Infinity y clamp a un rango grande (p.ej. ±1e6) para estabilidad.

3) Modelos económicos (replicar lógica del HTML)
A) Mercado
- Definir:
  - Pd0(q) = Eval(demandBase, q)
  - Ps0(q) = Eval(supplyBase, q)
  - Pd1(q) = Safe(Pd0(q) + ΔD)
  - Ps1(q) = Safe(Ps0(q) - ΔS)
- Equilibrio con impuesto (wedge):
  - Encontrar Q* resolviendo Pd1(Q) - (Ps1(Q) + t) = 0 con FindRoot(f) usando defaults low=0 high=1000
  - Precio consumidor Pc = Pd1(Q*)
  - Precio productor neto Pp = Pc - t
- Equilibrio sin impuesto (para DWL):
  - Q_noTax resolviendo Pd1(Q) - Ps1(Q) = 0
- Excedentes (por integración, con recortes a 0 como en el HTML):
  - CS = ∫_0^{Q*} max(0, Pd1(q) - Pc) dq
  - PS = ∫_0^{Q*} max(0, Pp - Ps1(q)) dq
  - TaxRev = t * Q*
  - DWL = ∫_{Q*}^{Q_noTax} max(0, Pd1(q) - Ps1(q)) dq

B) Empresa competitiva
- CT(q) = Eval(firmCostBase, q)
- CMg(q) = dCT/dq (derivada numérica)
- CMe(q) = CT(q)/q con salvaguarda si q≈0 (usar q=0.01)
- CMeV(q) como en el HTML:
  - CF = CT(0) (aprox. coste fijo)
  - CMeV(q) = (CT(q) - CF)/q si q>0.01; si no, 0
- Modo CP:
  - qOpt = FindRoot((q)=>CMg(q)-P, 0, 300)
  - Beneficio = P*qOpt - CT(qOpt)
- Modo LP:
  - qLP = FindRoot((q)=>CMg(q)-CMe(q), 0.1, 500) (mínimo de CMe aproximado por condición CMg=CMe)
  - pLP = CMe(qLP)

C) Monopolio
- Pd(q) = Eval(monoDemand, q)
- CT(q) = Eval(monoCost, q)
- IT(q) = Pd(q) * q
- IMg(q) = dIT/dq
- CMg(q) = dCT/dq
- Óptimo monopolio:
  - qm = FindRoot((q)=>IMg(q)-CMg(q), 0, 300)
  - pm = Pd(qm)
  - Beneficio = IT(qm) - CT(qm)
- Benchmark CP:
  - qCP = FindRoot((q)=>Pd(q)-CMg(q), 0, 300)
- DWL:
  - DWL = ∫_{qm}^{qCP} max(0, Pd(q)-CMg(q)) dq

D) Elasticidad (punto)
- Usar la demanda del mercado + shock demanda:
  - Pd(q) = Eval(demandBase, q) + ΔD
- Para precio P_el:
  - qEnP = FindRoot((q)=>Pd(q)-P_el, 0, 500)
  - dpdq = dPd/dq en qEnP
  - elasticidad = |(1/dpdq) * (P_el / qEnP)| (si dpdq≈0 o qEnP≈0 => reportar “no computable”)

Desktop (OfertaDemanda.Desktop) - implementar
- MainWindow con TabControl: Mercado, Empresa, Monopolio, Elasticidad.
- Inputs:
  - TextBox para expresiones.
  - Sliders con los rangos indicados.
  - Selector CP/LP en Empresa.
- Resultados:
  - Mostrar con formato numérico razonable (p.ej. 2 decimales) y unidades donde aplique.
  - Mostrar errores de parseo/cálculo en un panel visible (sin excepciones no controladas).
- Gráficos (LiveCharts2):
  - Mercado: curvas Pd0/Ps0 (opcionales como punteadas) y Pd1/Ps1; marcar equilibrio.
  - Empresa: CMg, CMe, CMeV y línea horizontal del precio P; marcar qOpt.
  - Monopolio: Pd, IMg, CMg; marcar qm y pm.
  - Elasticidad: Pd y marcador en (qEnP, P_el).

Tests (OfertaDemanda.Core.Tests)
- Parser: validar que se evalúan correctamente las expresiones por defecto del HTML:
  - "100 - 0.5q", "20 + 0.5q", "200 + 10q + 0.5q^2", etc.
- Numérica:
  - Root: f(q)=q-10 => raíz 10
  - Derivada: f(q)=q^2 => f'(5)=10 aprox
  - Integral: ∫_0^1 q dq = 0.5 aprox
- Mercado/Monopolio:
  - Caso simple con orden de magnitud coherente (no hace falta exactitud analítica perfecta; tolerancias).
- Asegurar que dotnet test pasa.

Entrega
- README con comandos:
  - dotnet restore / build / test / run
- La solución debe compilar y ejecutar abriendo ventana.

