# Sorteos Movil — Cliente

[![.NET Version](https://img.shields.io/badge/.NET-10.0-3F51B5?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-.NET%20MAUI-blue?logo=dotnet)](https://learn.microsoft.com/dotnet/maui/)
[![Database](https://img.shields.io/badge/Database-SQLite-003B57?logo=sqlite)](https://www.sqlite.org/)
[![Tests](https://img.shields.io/badge/Tests-67%20Passed%20(100%25)-success)](https://github.com/)
[![Target OS](https://img.shields.io/badge/OS-Android%20%7C%20Windows%20%7C%20iOS-brightgreen)](#requisitos-del-sistema)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20MVVM%20%2B%20SourceGen-orange)](#arquitectura-y-patrones-de-diseno)

Sorteos Movil es una aplicacion movil multiplataforma de grado empresarial desarrollada con **.NET MAUI (.NET 10)** y **C# 13** para la gestion integral, agil y autonoma de sorteos y rifas. Opera en modo 100% local, persistiendo toda la informacion en una base de datos **SQLite** embebida y transaccional, garantizando alta disponibilidad sin requerir conexion a internet ni servidores en la nube.

---

## Caracteristicas Principales

- **Tablero de Control (Dashboard Ejecutivo):** Metricas consolidadas en tiempo real: sorteos en juego, porcentaje de ocupacion de boletos, recaudacion total y cartera activa de clientes.
- **Sorteos de la Semana:** Carrusel interactivo de planificacion semanal con sincronizacion cultural (`es-MX`), deteccion automatica del dia actual, transicion semanal agnostica al tiempo y depuracion fisica transaccional en cascada.
- **Configuracion Flexible de Sorteos:** Emision a partir de plantillas con personalizacion de oportunidades (1 a 4+), rango de numeros (100, 500, 1000), costos unitarios, validacion de unicidad y asignacion multiple de premios.
- **Catalogo de Premios y Clientes:**
  - Premios en efectivo o en especie con proteccion estricta de integridad referencial.
  - Cartera de clientes con asignacion de numeros fijos ("de planta") y verificacion de unicidad.
- **Cuadricula Interactiva de Boletos y Reservas:**
  - Mapa visual de alta velocidad codificado por estados (Disponible, Apartado, Pagado, Ganador).
  - Agrupacion automatica de oportunidades en boletos consolidados (ej. `01 - 51`) mediante algoritmo matematico de paso continuo.
  - Carga progresiva (*lazy loading*) con ventana observable para prevenir congelamientos en el hilo de interfaz (`UI Thread`).
  - Asignacion al azar sobre disponibilidad real, gestion de comprobantes y liberacion instantanea.
  - Integracion directa con WhatsApp (`wa.me`) para confirmacion y envio de fichas de apartado.
- **Motor de Combos Inteligente:** Algoritmo desacoplado para adjudicacion automatica de numeros de planta, resolucion de colisiones entre clientes y disolucion de grupos en conflicto.
- **Seguridad y Control de Acceso:** Modulo de bloqueo por PIN de seguridad de 4 digitos respaldado con almacenamiento seguro en `SecureStorage`, gestion reactiva de permisos del dispositivo y terminos de uso legales integrados.

---

## Stack Tecnologico

| Componente | Tecnologia / Libreria | Rol en el Proyecto |
|---|---|---|
| **Framework Base** | .NET 10 (.NET MAUI) | Desarrollo movil multiplataforma nativo (Android, Windows, iOS) |
| **Lenguaje** | C# 13 | Tipado estricto con `Nullable`, `partial properties` y Source Generators |
| **Patron Arquitectonico** | MVVM | Separacion estricta entre vista y logica de presentacion |
| **Toolkit MVVM** | `CommunityToolkit.Mvvm` | Generacion de comandos `[RelayCommand]` y reactividad en tiempo de compilacion |
| **Toolkit UI** | `CommunityToolkit.Maui` | Conversores, comportamientos de plataforma y componentes visuales avanzados |
| **Base de Datos** | SQLite (`sqlite-net-pcl` + `SQLitePCLRaw`) | Persistencia relacional local con soporte ACID y modo WAL |
| **Seguridad de Datos** | `Microsoft.Maui.Storage` | Credenciales y PIN cifrados en `SecureStorage` (Keystore/Keychain) |
| **Pruebas Automatizadas** | xUnit (.NET 10) | Suite empresarial de 67 pruebas unitarias, de integracion y reactividad |
| **Renderizado XAML** | `MauiXamlInflator=SourceGen` | Compilacion de vistas XAML a codigo C# nativo para arranque optimizado |

---

## Arquitectura y Patrones de Diseno

La solucion se rige por principios de Clean Architecture y directivas de alto rendimiento:

1. **Desacoplamiento de Persistencia y Presentacion:**
   - La persistencia fisica en SQLite se gestiona mediante entidades de base de datos explicitas (`DiaSemanaEntity`), mientras que la capa visual interactua con modelos observables puros (`DiaSemanaItem`, `NumeroGridItem`), eliminando sobrecostos del ORM en el arbol visual.
2. **Abstracciones de Servicios UI:**
   - ViewModels 100% desacoplados del contexto de UI mediante `IDialogService` (alertas y confirmaciones) e `INavigationService` (navegacion Shell y paso de parametros tipados), permitiendo pruebas unitarias completas sin mocks de plataforma.
3. **Optimizacion de Base de Datos SQLite:**
   - Modo WAL obligatorio (`PRAGMA journal_mode = WAL;`) para lecturas y escrituras concurrentes sin interbloqueos.
   - Indices explicitos (`[Indexed]`) en columnas de filtrado recurrente (`NumeroReservado.IdEstatus`, `SorteoPlantilla.IdEstatusSorteo`).
   - Consultas agregadas nativas directas en motor C para calculo de maximos y metricas, eliminando el volcado de tablas completas a RAM (`.ToListAsync()`).
   - Integridad referencial con `PRAGMA foreign_keys = ON;` y control de esquema con `PRAGMA user_version = 1;`.
4. **Mutaciones Atomicas y Reactividad:**
   - Prohibicion de recargas ciegas globales; la sincronizacion entre vistas se resuelve mediante mensajeria desacoplada (`WeakReferenceMessenger`) con altas, bajas y modificaciones puntuales en `ObservableCollection`.
5. **Ciberseguridad Movil (OWASP MASVS):**
   - Proteccion de base de datos local contra extraccion no autorizada (`android:allowBackup="false"`).
   - Secretos y estados de autenticacion confinados a `SecureStorage`.

---

## Suite de Pruebas Automatizadas

El proyecto incluye una suite exhaustiva de pruebas automatizadas bajo **xUnit** en el proyecto `Sorteos.Cliente.Movil.Tests`:

```bash
dotnet test Sorteos.Cliente.Movil.Tests/Sorteos.Cliente.Movil.Tests.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

### Cobertura de Pruebas (67 Pruebas Aprobadas - 100% de Exito):
- **Integracion SQLite ACID:** Ciclo de vida de base de datos, consultas con filtros, unicidad y eliminacion en cascada.
- **Logica de Sorteos:** Generacion de numeros, apartados individuales y masivos, cobros y adjudicacion de ganadores.
- **Reglas de Combos y Plantas:** Distribucion de oportunidades, colisiones entre clientes y disolucion de combos.
- **Reactividad MVVM:** Mutaciones atomicas de colecciones sin reinicializacion visual.
- **Seguridad y Configuracion:** Autenticacion por PIN, respaldo en `SecureStorage` y generacion de fichas bancarias.

---

## Requisitos del Sistema

- **SDK:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) instalado.
- **Carga de trabajo MAUI:**
  ```bash
  dotnet workload install maui
  ```
- **IDE:** Visual Studio 2022 (v17.12+) o Visual Studio Code con extension .NET MAUI.
- **Plataformas Soportadas:** Android 5.0 (API 21) o superior, Windows 10/11, iOS 15.0+.

---

## Compilacion y Ejecucion

### Android (Dispositivo o Emulador):
```bash
dotnet build -t:Run -f net10.0-android Sorteos.Cliente.Movil/Sorteos.Cliente.Movil.csproj
```

### Generacion de APK Release:
```bash
dotnet publish Sorteos.Cliente.Movil/Sorteos.Cliente.Movil.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

### Windows (Escritorio):
```bash
dotnet build -t:Run -f net10.0-windows10.0.19041.0 Sorteos.Cliente.Movil/Sorteos.Cliente.Movil.csproj
```

---

## Estructura del Repositorio

```
Sorteos.Cliente.Movil/
├── Sorteos.Cliente.Movil.slnx          # Solucion .NET 10
├── README.md                           # Documentacion publica del proyecto
├── Sorteos.Cliente.Movil.Tests/         # Suite empresarial de pruebas xUnit (67 pruebas)
└── Sorteos.Cliente.Movil/              # Aplicacion principal .NET MAUI
    ├── App.xaml / AppShell.xaml        # Ciclo de vida y navegacion Shell
    ├── MauiProgram.cs                  # Composicion de inyeccion de dependencias (DI)
    ├── Models/                         # Entidades SQLite fisicas y modelos de presentacion UI
    ├── Services/                       # Repositorio SQLite, reglas de negocio y abstracciones
    ├── ViewModels/                     # Capa de presentacion reactiva (CommunityToolkit.Mvvm)
    ├── Views/                          # Interfaces XAML con Compiled Bindings (x:DataType)
    ├── Converters/ / Helpers/          # Conversores de valor y utilidades de validacion
    └── Platforms/                      # Configuracion nativa (Android, Windows, iOS)
```

---

## Servicios Profesionales y Contacto

Este proyecto forma parte de mi portafolio publico de desarrollo de software profesional. Si buscas un desarrollador especializado para:

- **Desarrollo de Aplicaciones Moviles:** Creacion de aplicaciones nativas de alto rendimiento con .NET MAUI o Xamarin para Android, iOS y Windows.
- **Arquitectura de Software y Refactorizacion:** Diseno de arquitecturas limpias (Clean Architecture, MVVM, DDD), inyeccion de dependencias y migracion de sistemas legados.
- **Optimizacion de Bases de Datos Locales:** Hardening de SQLite, prevencion de bloqueos en hilo de UI, transacciones ACID complejas y consultas optimizadas.
- **Desarrollo Backend e Integraciones:** Creacion de APIs RESTful con ASP.NET Core, integraciones de pasarelas de pago y mensajeria automatizada.

### Canales de Contacto Directo:

- **LinkedIn:** [Tu Perfil Profesional](https://www.linkedin.com/) *(Actualizar con tu enlace)*
- **Correo Electronico:** `tu-correo@dominio.com` *(Actualizar con tu correo de contacto)*
- **Telegram / WhatsApp:** Enlace de mensajeria directa *(Actualizar si deseas incluirlo)*
- **Perfil de GitHub:** [github.com](https://github.com/) *(Actualizar con tu usuario)*

*Disponible para contratacion por proyecto, consultoria tecnica o posiciones de desarrollo remoto.*
