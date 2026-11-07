# Configuración y despliegue de Amor Bajo Lupa en Somee

Esta rama utiliza **ASP.NET MVC 5 y .NET Framework 4.7.2**. Somee indica que admite aplicaciones MVC 5 sobre .NET Framework 4.0–4.8. Para MVC clásico, configura el Application Pool con **Managed Pipeline Mode = Integrated** y **.NET CLR v4.0**.

## 1. Ajustes del sitio en Somee

En el panel de la cuenta propietaria del sitio, verifica:

- ASP.NET / .NET Framework compatible con 4.7.2 (Somee puede ofrecer una opción 4.x compatible, por ejemplo 4.8).
- Managed Pipeline Mode: **Integrated**.
- .NET CLR: **v4.0**.
- SSL activo y el sitio accesible mediante HTTPS, necesario para el retorno seguro de Google OAuth.

El sitio alojado en el proyecto de referencia del ZIP es `https://amorbajolupa.somee.com/`. Confirma que ese siga siendo el dominio asignado antes de configurar las URLs de Google.

## 2. Cómo se leen las credenciales y la conexión SQL

La configuración está separada en archivos para que las contraseñas no se guarden en GitHub:

- `TestPatrones/Web.config`: archivo privado principal, excluido de Git.
- `TestPatrones/App_Data/GoogleAuth.secrets.config`: Client ID y Client Secret de Google.
- `TestPatrones/App_Data/SqlServer.secrets.config`: cadena de conexión de SQL Server de Somee.

El `.gitignore` excluye los tres archivos privados. En GitHub solo se guardan las plantillas `.example`.

### Crear los archivos privados

Dentro de la carpeta `TestPatrones/App_Data`, crea dos copias locales:

1. Copia `GoogleAuth.secrets.config.example` como `GoogleAuth.secrets.config`.
2. Copia `SqlServer.secrets.config.example` como `SqlServer.secrets.config`.

Rellena las copias privadas con los valores reales proporcionados por la propietaria de Somee y el cliente OAuth que se usará en producción. No edites las plantillas `.example` para pegar contraseñas.

El archivo privado de Google debe tener la sección `appSettings` y estas dos claves:

```xml
<appSettings>
  <add key="GoogleClientId" value="CLIENT_ID_REAL" />
  <add key="GoogleClientSecret" value="CLIENT_SECRET_REAL" />
</appSettings>
```

El archivo privado de SQL debe tener la sección `connectionStrings` y una entrada con el nombre exacto `AmorBajoLupa`:

```xml
<connectionStrings>
  <add name="AmorBajoLupa"
       providerName="System.Data.SqlClient"
       connectionString="Data Source=SERVIDOR_SQL;Initial Catalog=BASE_DATOS;User ID=USUARIO_SQL;Password=CLAVE_SQL;Integrated Security=False;Connect Timeout=30" />
</connectionStrings>
```

Usa los valores exactos del panel de Somee. No inventes el host, catálogo, usuario ni contraseña. La conexión remota de Somee normalmente utiliza autenticación SQL; por eso el ejemplo emplea `Integrated Security=False`.

### Referencias en el Web.config principal

El `Web.config` privado debe contener estas referencias:

```xml
<connectionStrings configSource="App_Data\SqlServer.secrets.config" />

<appSettings file="App_Data\GoogleAuth.secrets.config">
  <add key="webpages:Version" value="3.0.0.0" />
  <add key="webpages:Enabled" value="false" />
  <add key="ClientValidationEnabled" value="true" />
  <add key="UnobtrusiveJavaScriptEnabled" value="true" />
</appSettings>
```

Conserva también las secciones de MVC, .NET Framework, ensamblados y CodeDom que necesita el proyecto. La plantilla versionada `TestPatrones/Web.config.example` muestra la estructura de referencia. No reemplaces a ciegas el Web.config privado que ya utiliza Visual Studio.

La aplicación obtiene la conexión con `ConfigurationManager.ConnectionStrings["AmorBajoLupa"]`. El inicio de sesión lee `GoogleClientId` y `GoogleClientSecret` de `ConfigurationManager.AppSettings`; opcionalmente, las variables de entorno `GOOGLE_CLIENT_ID` y `GOOGLE_CLIENT_SECRET` tienen prioridad.

Microsoft documenta que `configSource` permite guardar la sección `connectionStrings` en un archivo externo, mientras `appSettings file` permite separar valores de configuración. La carga en tiempo de ejecución es transparente para `ConfigurationManager`.

**Los archivos privados reales no se incluyen en el ZIP de GitHub que se revisó.** El ZIP contenía las plantillas `.example` y referencias a los archivos privados, pero no los archivos con las credenciales. Deben obtenerse directamente de la propietaria del hosting o recrearse con los datos oficiales de Somee y Google Cloud.

## 3. Google OAuth para el dominio de Somee

En Google Cloud Console, abre el cliente OAuth de tipo **Web application** que se utilizará en producción. Añade el origen HTTPS del dominio real a **Authorized JavaScript origins**.

Para el dominio de referencia, el origen es:

```text
https://amorbajolupa.somee.com
```

El `Startup.cs` usa actualmente `CallbackPath = "/signin-google"`. Por eso el URI de retorno autorizado debe ser:

```text
https://amorbajolupa.somee.com/signin-google
```

Si la propietaria utiliza otro dominio o un dominio personalizado, reemplaza ambas URLs por el dominio real. Conserva también las URLs locales de desarrollo que todavía necesites. El Client ID y el Client Secret deben pertenecer al mismo cliente OAuth.

## 4. Publicación de archivos

1. Descarga la rama que se haya aprobado para despliegue.
2. Abre `TestPatrones.sln` en Visual Studio 2019.
3. Restaura los paquetes NuGet.
4. Compila la solución en configuración **Release**.
5. Abre **Build > Publish** y crea un perfil **Folder**.
6. Publica en una carpeta local.
7. Revisa que el resultado incluya `bin`, `Content`, `Scripts`, `Views`, `Global.asax` y un `Web.config` configurado para usar los dos archivos externos.
8. La publicación no lleva las credenciales privadas porque esos archivos están excluidos del repositorio/proyecto publicable. Súbelos por separado al directorio `App_Data` del sitio Somee mediante un canal seguro.
9. Sube el resto de los archivos publicados a la raíz del sitio usando File Manager o FTP/FileZilla.
10. No reemplaces el `Web.config` privado de producción con uno que contenga marcadores. Conserva un respaldo antes de modificarlo.
11. No borres `.well-known` ni otros archivos de alojamiento que no formen parte del despliegue.

Somee ofrece File Manager en el panel y admite aplicaciones MVC 5 con el Application Pool configurado en Integrated.

## 5. Comprobaciones después de publicar

Prueba en el dominio HTTPS real:

- Portada, imágenes, CSS y JavaScript.
- Navegación entre secciones.
- Los cuatro tests.
- Inicio de sesión con Google en los dos tests avanzados.
- Guardado y lectura de resultados en la base SQL Server alojada en Somee.
- Primer artículo: `/Home/ArticuloConocerse`.
- Acceso directo y actualización del navegador en rutas MVC.

Si la portada abre pero las rutas MVC devuelven 404, revisa el Application Pool Integrated y la sección `system.webServer/handlers` del Web.config. Si falla la base de datos, valida la cadena privada `AmorBajoLupa`; si falla Google, revisa las credenciales externas y la URL de retorno exacta.

No se ha realizado una publicación en Somee desde este repositorio. El despliegue debe hacerlo la propietaria del hosting y probarse con sus credenciales y su base de datos reales.
