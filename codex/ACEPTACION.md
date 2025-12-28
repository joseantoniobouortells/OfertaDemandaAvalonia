# Criterios de aceptación (MVP)

1) Compila y ejecuta en .NET 8 (macOS/Windows/Linux):
   - dotnet restore
   - dotnet build
   - dotnet test
   - dotnet run (abre ventana Avalonia)

2) Arquitectura:
   - Solución con 3 proyectos:
     - src/OfertaDemanda.Desktop (Avalonia UI, MVVM)
     - src/OfertaDemanda.Core (motor económico + matemático)
     - test/OfertaDemanda.Core.Tests (xUnit)
   - Cálculo y numérica en Core. Code-behind mínimo.

3) Funcionalidad equivalente al HTML (MVP):
   - 4 pestañas: Mercado / Empresa / Monopolio / Elasticidad
   - Inputs de funciones como texto con variable q:
     - Mercado: Pd(q), Ps(q), shocks ΔD y ΔS, impuesto unitario t
     - Empresa: CT(q), precio P, modo CP y LP
     - Monopolio: Pd(q) y CT(q)
     - Elasticidad: Pd(q) y precio P
   - Resultados:
     - Mercado: Q*, Pc (consumidor), Pp (productor), CS, PS, recaudación, DWL
     - Empresa: q* CP, q_LP (si aplica), series CMg/CMe/CMeV (o CMeV opcional documentado)
     - Monopolio: q_m, p_m, beneficio, DWL estimada
     - Elasticidad: Q(P) y elasticidad punto
   - Errores: expresión inválida o sin raíz => mensaje claro, sin crashear.

4) Gráficos (nativos):
   - Usar LiveCharts2 para Avalonia (SkiaSharp).
   - Curvas + puntos (sin sombreado obligatorio en MVP).

