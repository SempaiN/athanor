@echo off
REM Crea el keystore de firma para Google Play. EJECUTALO UNA SOLA VEZ.
REM Guarda el archivo y las contraseñas en un lugar seguro: si los perdés,
REM no podrás actualizar la app en Play Store.

set KEYTOOL="C:\Program Files\Unity\Hub\Editor\6000.0.78f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
set OUT=%~dp0..\release.keystore

echo Creando keystore en: %OUT%
echo Te va a pedir una contraseña (anotala) y unos datos identificativos.
echo.

%KEYTOOL% -genkeypair -v -keystore "%OUT%" -alias athanor -keyalg RSA -keysize 2048 -validity 10950

echo.
echo LISTO. Ahora define estas variables de entorno antes de compilar el AAB:
echo    ATHANOR_KEYSTORE_PASS  (la contraseña del keystore)
echo    ATHANOR_KEY_PASS       (la contraseña de la clave, normalmente la misma)
echo El archivo release.keystore NO se sube a git (esta en .gitignore).
pause
