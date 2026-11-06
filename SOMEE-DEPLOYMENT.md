# Despliegue de Amor Bajo Lupa en Somee

Esta aplicación es ASP.NET MVC 5 sobre .NET Framework 4.7.2. Somee documenta compatibilidad con MVC 5 y .NET Framework 4.0–4.8. Para MVC clásico, la aplicación debe ejecutarse con el Application Pool en modo **Integrated** y CLR **v4.0**.

## 1. Configuración del sitio en Somee

En el panel de Somee, abre las propiedades del sitio y verifica:

- ASP.NET / .NET Framework compatible con **4.7.2** (Somee puede ofrecer 4.7 o 4.8 según el servidor).
- Application Pool: **Managed Pipeline Mode = Integrated**.
- .NET CLR: **v4.0**.
- SSL activo y el sitio accesible mediante HTTPS, especialmente porque se utiliza Google OAuth.

No cambies la versión del framework del proyecto para que coincida con el número que ves en IIS: .NET Framework 4.x utiliza CLR v4.0.

## 2. Web.config privado

El archivo `TestPatrones/Web.config` está excluido de Git mediante `.gitignore` porque contiene secretos. La plantilla versionada es `TestPatrones/Web.config.example`.

No reemplaces el Web.config completo de Visual Studio por la plantilla sin revisar las secciones existentes. Integra en el Web.config privado:

- La cadena de conexión `AmorBajoLupa` usando el servidor, base de datos, usuario y contraseña SQL que muestra Somee.
- Las claves `GoogleClientId` y `GoogleClientSecret` del mismo cliente OAuth tipo **Web application**.
- La configuración `system.web` para target framework 4.7.2 y UTF-8.
- La configuración `system.webServer` para rutas MVC sin extensión, tal como aparece en la plantilla.

La conexión SQL alojada en Somee normalmente utiliza autenticación SQL. No uses `Integrated Security=True` para la base de datos remota salvo que Somee indique expresamente que tu plan y base de datos requieren ese mecanismo.

Nunca subas el Web.config real, contraseñas, Client Secret ni credenciales FTP a GitHub.

## 3. Google OAuth en producción

En Google Cloud Console, el cliente OAuth de Amor Bajo Lupa debe incluir el origen HTTPS real del sitio en **Authorized JavaScript origins** y la URL exacta de retorno en **Authorized redirect URIs**.

Con el callback actual de OWIN en `Startup.cs`, el URI de retorno termina en:

```text
https://TU-SITIO.somee.com/signin-google
```

Sustituye `TU-SITIO.somee.com` por el dominio real asignado al sitio (o el dominio personalizado que esté configurado). No registres una URL inventada ni elimines las direcciones locales de desarrollo si todavía las necesitas.

El código lee las claves `GoogleClientId` y `GoogleClientSecret` desde `appSettings`. Ambas credenciales tienen que pertenecer al mismo cliente OAuth.

## 4. Publicar desde Visual Studio 2019

1. Cambia a la rama que se haya aprobado para desplegar.
2. Compila la solución en configuración **Release**.
3. En Visual Studio, abre **Build > Publish**.
4. Crea un perfil de publicación con destino **Folder**.
5. Publica en una carpeta local de salida.
6. Comprueba que la salida contenga, entre otros, `bin`, `Content`, `Scripts`, `Views`, `Global.asax` y el `Web.config` correcto.
7. Sube el contenido publicado a la carpeta raíz del sitio Somee mediante el File Manager de Somee o FTP/FileZilla. En FTP, entra en la carpeta del sitio indicada por Somee; no cargues los archivos en una carpeta superior.
8. No subas `.git`, `.vs`, archivos `.user`, copias de seguridad ni archivos de credenciales locales.

Somee documenta tanto la publicación a carpeta desde Visual Studio seguida de carga por FTP, como el uso de su File Manager.

## 5. Comprobaciones después de publicar

Probar en el dominio HTTPS real:

- La portada y sus imágenes.
- Navegación entre secciones.
- Los cuatro tests y sus rutas.
- Inicio de sesión con Google en los dos tests avanzados.
- Guardado de resultados en SQL Server.
- El primer artículo: `/Home/ArticuloConocerse`.
- Acceso directo a rutas MVC (por ejemplo, refrescar una página de artículo).

Si una ruta MVC devuelve 404 pero la portada abre, revisa primero el Application Pool Integrated y la sección `system.webServer/handlers` del Web.config.

No se ha realizado una publicación en Somee desde este repositorio. El despliegue debe probarse en el sitio real con las credenciales y la base de datos que ya administra su responsable.
