# 💜 Amor Bajo Lupa

**Amor Bajo Lupa** es una plataforma web interactiva diseñada para explorar patrones de comportamiento, emociones, expectativas y dinámicas presentes en las relaciones de pareja.

Su objetivo es ofrecer experiencias de autoconocimiento mediante tests interactivos cuyos resultados tienen carácter orientativo.

## ✨ Características principales

- Cinco experiencias interactivas de evaluación.
- Preguntas sobre emociones, comunicación, confianza y comportamiento.
- Resultados personalizados según las respuestas.
- Registro de resultados e intentos en una base de datos.
- Inicio de sesión con Google en las experiencias que lo requieren.
- Tests avanzados con acceso mediante cuenta de Google.
- Acceso premium mediante Mercado Pago.
- Interfaz visual adaptable con colores suaves.

## 🧠 Tests disponibles

1. **¿Qué está ocultando?**  
   Explora comportamientos relacionados con la confianza, la comunicación y los límites en una relación.

2. **Descubre tus Patrones**  
   Permite explorar las respuestas emocionales, expectativas y hábitos personales.

3. **¿Qué está ocultando? (Avanzado)**  
   Una experiencia más elaborada para profundizar en los comportamientos dentro de una relación.

4. **Descubre tus Patrones (Avanzado)**  
   Una evaluación orientada a explorar con mayor profundidad los patrones personales y emocionales.

5. **¿Estás listo realmente para una relación?**  
   Una experiencia premium que permite explorar aspectos relacionados con la preparación emocional para una relación.

## 🛠️ Tecnologías utilizadas

- C#
- ASP.NET MVC 5
- .NET Framework 4.7.2
- HTML5
- CSS3
- JavaScript
- SQL Server
- Google OAuth 2.0
- Mercado Pago
- Bootstrap
- Visual Studio 2019

## ⚙️ Configuración del proyecto

### Requisitos

- Windows.
- Visual Studio 2019.
- .NET Framework 4.7.2.
- SQL Server.
- Una base de datos compatible con la estructura del proyecto.

### Instalación

1. Clonar este repositorio:

```bash
git clone https://github.com/sewas-creator/Amor-Bajo-Lupa.git
```

2. Abrir `TestPatrones.sln` con Visual Studio 2019.

3. Preparar los archivos privados de configuración a partir de los archivos de ejemplo.

### Configuración de SQL Server

Dentro de `App_Data`, copiar:

`SqlServer.secrets.config.example`

Crear una copia llamada:

`SqlServer.secrets.config`

Configurar la cadena de conexión con los datos de tu propio servidor SQL Server.

### Configuración de Google y Mercado Pago

Dentro de `App_Data`, copiar:

`GoogleAuth.secrets.config.example`

Crear una copia llamada:

`GoogleAuth.secrets.config`

Completar los valores correspondientes con tus propias credenciales de Google OAuth y Mercado Pago.

### Configuración de Web.config

Utilizar `Web.config.example` como referencia y preparar el archivo `Web.config` con las referencias a los archivos privados de configuración.

### Base de datos

Crear o restaurar una base de datos compatible con la estructura de Amor Bajo Lupa.

El proyecto utiliza tablas como:

- `Usuarios`
- `Tests`
- `Resultados`
- `Intentos`
- `ComprasTestPremium`

4. Compilar la solución.
5. Ejecutar el proyecto desde Visual Studio.

## 🌐 Publicación

El proyecto está publicado en Somee:

https://amorbajolupaweb.somee.com

Para configurar una publicación FTP propia, se incluye el archivo de referencia:

`Properties/PublishProfiles/FTPProfile.pubxml.example`

Debes crear y configurar tu propio perfil de publicación con los datos de tu alojamiento.

## 🔐 Seguridad

Las credenciales privadas y los datos de configuración local no deben almacenarse en el repositorio público.

Los archivos privados están excluidos mediante `.gitignore`.

Nunca publiques contraseñas, tokens de Mercado Pago, credenciales de Google OAuth ni cadenas de conexión reales.

## ⚠️ Aviso importante

Los resultados de Amor Bajo Lupa son orientativos y están destinados al autoconocimiento. No constituyen diagnósticos psicológicos ni pruebas definitivas sobre la conducta o fidelidad de una persona.

## 📌 Versión actual

**v0.9.0 (Beta)**

Proyecto en desarrollo.

---

Desarrollado como proyecto tecnológico independiente bajo el nombre **Amor Bajo Lupa**.
