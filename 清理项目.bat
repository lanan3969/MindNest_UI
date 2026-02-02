@echo off
chcp 65001 > nul
echo ====================================
echo MindNest 项目清理工具
echo ====================================
echo.
echo 此脚本将删除以下可重新生成的文件：
echo - Unity Library 文件夹（~2GB）
echo - Unity Temp 文件夹
echo - Unity Logs 文件夹
echo - Node modules 文件夹（~200MB）
echo - Python 缓存文件
echo - 备份图片文件夹
echo.
echo 警告：删除后首次打开Unity会重新生成Library（需要几分钟）
echo       Node modules可以通过 npm install 重新安装
echo.
set /p confirm="确认删除？(Y/N): "
if /i not "%confirm%"=="Y" (
    echo 已取消清理
    pause
    exit /b
)

echo.
echo 开始清理...
echo.

REM 删除Unity缓存
if exist "03_MR_Experience\MindNest_MR\Library\" (
    echo [1/6] 删除 Unity Library...
    rmdir /s /q "03_MR_Experience\MindNest_MR\Library"
    echo ✓ Unity Library 已删除
) else (
    echo [1/6] Unity Library 不存在，跳过
)

if exist "03_MR_Experience\MindNest_MR\Temp\" (
    echo [2/6] 删除 Unity Temp...
    rmdir /s /q "03_MR_Experience\MindNest_MR\Temp"
    echo ✓ Unity Temp 已删除
) else (
    echo [2/6] Unity Temp 不存在，跳过
)

if exist "03_MR_Experience\MindNest_MR\Logs\" (
    echo [3/6] 删除 Unity Logs...
    rmdir /s /q "03_MR_Experience\MindNest_MR\Logs"
    echo ✓ Unity Logs 已删除
) else (
    echo [3/6] Unity Logs 不存在，跳过
)

if exist "03_MR_Experience\MindNest_MR\obj\" (
    echo [4/6] 删除 Unity obj...
    rmdir /s /q "03_MR_Experience\MindNest_MR\obj"
    echo ✓ Unity obj 已删除
) else (
    echo [4/6] Unity obj 不存在，跳过
)


REM 删除Python缓存
if exist "01_Backend_AI\__pycache__\" (
    echo [5/6] 删除 Python 缓存...
    rmdir /s /q "01_Backend_AI\__pycache__"
    echo ✓ Python 缓存已删除
) else (
    echo [5/6] Python 缓存不存在，跳过
)

REM 删除备份文件夹
if exist "01_Backend_AI\assets\nomi\backup\" (
    echo [6/6] 删除备份图片...
    rmdir /s /q "01_Backend_AI\assets\nomi\backup"
    echo ✓ 备份图片已删除
) else (
    echo [6/6] 备份图片不存在，跳过
)

echo.
echo ====================================
echo 清理完成！
echo ====================================
echo.
echo 注意事项：
echo 1. 首次打开Unity项目时会重新生成Library（约3-5分钟）
echo 2. 运行移动端应用前需要执行: cd 02_Mobile_App ^&^& npm install
echo 3. 如需恢复，请重新生成对应的文件
echo.
pause

