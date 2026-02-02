/**
 * MindNest Mobile App - Main Entry
 * =================================
 * 
 * React主应用，管理路由和全局状态
 * 
 * 功能：
 * 1. 底部导航栏（Report, Plant, Record）
 * 2. 页面路由切换
 * 3. API状态管理
 * 
 * 作者: MindNest Team
 * 日期: 2026-01-26
 */

import React, { useState, useEffect } from 'react';
import Welcome from './components/Welcome';
import DiaryRecord from './components/DiaryRecord';
import Report from './components/Report';
import Plant from './components/Plant';
import './App.css';

function App() {
    const [showWelcome, setShowWelcome] = useState(false);
    const [currentPage, setCurrentPage] = useState('record');
    const [userId] = useState('user_demo_001'); // 实际应从认证系统获取

    // 检查是否首次访问
    useEffect(() => {
        const welcomeCompleted = localStorage.getItem('mindnest_welcome_completed');
        if (!welcomeCompleted) {
            setShowWelcome(true);
        }
    }, []);

    const handleWelcomeComplete = (userData) => {
        console.log('✨ Welcome completed with user data:', userData);
        setShowWelcome(false);
        // 可以根据用户选择设置全局主题或其他配置
    };

    const renderPage = () => {
        switch (currentPage) {
            case 'report':
                return <Report userId={userId} />;
            case 'plant':
                return <Plant userId={userId} />;
            case 'record':
            default:
                return <DiaryRecord userId={userId} />;
        }
    };

    // 如果需要显示欢迎页，渲染Welcome组件
    if (showWelcome) {
        return <Welcome onComplete={handleWelcomeComplete} />;
    }

    return (
        <div className="min-h-screen bg-cream flex flex-col">
            {/* Main Content */}
            <main className="flex-1 pb-20">
                {renderPage()}
            </main>

            {/* Bottom Navigation */}
            <nav className="fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 px-6 py-3 flex justify-around items-center shadow-lg">
                <button
                    onClick={() => setCurrentPage('report')}
                    className={`flex flex-col items-center space-y-1 transition-colors ${currentPage === 'report' ? 'text-sage-green' : 'text-gray-400'
                        }`}
                >
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                    </svg>
                    <span className="text-xs font-medium">report</span>
                </button>

                <button
                    onClick={() => setCurrentPage('plant')}
                    className={`flex flex-col items-center space-y-1 transition-colors ${currentPage === 'plant' ? 'text-sage-green' : 'text-gray-400'
                        }`}
                >
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
                    </svg>
                    <span className="text-xs font-medium">plant</span>
                </button>

                <button
                    onClick={() => setCurrentPage('record')}
                    className={`flex flex-col items-center space-y-1 transition-colors ${currentPage === 'record' ? 'text-sage-green' : 'text-gray-400'
                        }`}
                >
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                    <span className="text-xs font-medium">record</span>
                </button>
            </nav>
        </div>
    );
}

export default App;
