# Builds en la nube (GitHub Actions)

El workflow [`build-apk.yml`](../.github/workflows/build-apk.yml) compila el APK en GitHub
usando [game-ci](https://game.ci/). Está en modo **manual** (no corre solo) hasta que
configures la licencia de Unity como secretos del repo — un paso que solo puede hacer el
dueño de la cuenta, una única vez.

## Configurar los secretos (una sola vez, ~10 min)

Unity Personal es gratis también en CI, pero hay que darle la licencia:

1. Entrá a https://github.com/SempaiN/athanor/settings/secrets/actions → **New repository secret**.
2. Creá estos 3 secretos:
   - `UNITY_EMAIL` → el email de tu cuenta Unity
   - `UNITY_PASSWORD` → tu contraseña de Unity
   - `UNITY_LICENSE` → el contenido del archivo de licencia `.ulf` (ver abajo)
3. El `.ulf` está en tu PC (se creó cuando activaste la licencia Personal):
   `C:\ProgramData\Unity\Unity_lic.ulf` o `%LOCALAPPDATA%\Unity\licenses\`.
   Abrilo con el Bloc de notas y pegá TODO su contenido como valor del secreto.
   Si no existe, seguí la guía oficial: https://game.ci/docs/github/activation

## Lanzar un build en la nube

1. GitHub → pestaña **Actions** → workflow **Build APK** → **Run workflow**.
2. (Opcional) En `publish_release` poné un tag (ej. `v2.0.0`) para que además
   publique el APK como GitHub Release; vacío = solo genera el artefacto descargable.
3. El primer build tarda ~25–40 min (después el caché de Library lo baja mucho).

## Mientras tanto

Los builds locales siguen funcionando igual (`BuildScript.BuildApk`), que es lo que se
usó para todas las releases hasta ahora. El CI es un plus: builds sin PC a mano.
