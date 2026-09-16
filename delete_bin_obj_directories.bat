@echo off
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S bin obj 2^>NUL') DO (
    IF EXIST "%%G" RMDIR /S /Q "%%G"
)
