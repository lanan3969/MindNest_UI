@echo off
chcp 65001 >nul
echo ========================================
echo   MindNest 一键启动脚本
echo ========================================
echo.

echo [1/2] 启动后端服务 (Python FastAPI)...
echo 后端地址: http://localhost:8000
echo 文档地址: http://localhost:8000/docs
echo.
start "MindNest Backend" cmd /k "cd /d %~dp0\01_Backend_AI && python main.py"
timeout /t 3 >nul

echo [2/2] 启动前端应用 (React)...
echo 前端地址: http://localhost:3000
echo.
start "MindNest Frontend" cmd /k "cd /d %~dp0\02_Mobile_App && npm start"

echo.
echo ========================================
echo   ✅ MindNest 已启动！
echo ========================================
echo   后端: http://localhost:8000
echo   前端: http://localhost:3000
echo   文档: http://localhost:8000/docs
echo ========================================
echo.
echo 按任意键关闭此窗口...
pause >nul
