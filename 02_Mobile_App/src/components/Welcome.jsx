/**
 * MindNest - Welcome Onboarding Component
 * =========================================
 * 
 * 三段式欢迎引导页面，建立个人连接并了解用户需求
 * 
 * 流程：
 * 1. 情感切入 - 选择当前心情状态
 * 2. 场景定义 - 识别来此的原因（可多选）
 * 3. 仪式感开启 - 设定疗愈目标
 * 
 * 作者: MindNest Team
 * 日期: 2026-01-30
 */

import React, { useState } from 'react';

const Welcome = ({ onComplete }) => {
    const [step, setStep] = useState(0);  // 从0开始，0是基本信息收集
    const [nickname, setNickname] = useState('');
    const [gender, setGender] = useState('');
    const [birthday, setBirthday] = useState('');
    const [selectedMood, setSelectedMood] = useState(null);
    const [selectedReasons, setSelectedReasons] = useState([]);
    const [selectedGoal, setSelectedGoal] = useState(null);

    // 步骤0：性别选项
    const genderOptions = [
        { id: 'male', label: '男生', icon: '👦', color: 'from-blue-100 to-blue-50' },
        { id: 'female', label: '女生', icon: '👧', color: 'from-pink-100 to-pink-50' },
        { id: 'other', label: '其他', icon: '✨', color: 'from-purple-100 to-purple-50' },
        { id: 'prefer-not-say', label: '不便透露', icon: '🤐', color: 'from-gray-100 to-gray-50' }
    ];

    // 步骤1：情感切入选项
    const moodOptions = [
        { 
            id: 'growth', 
            icon: '🌱', 
            label: '期待成长',
            color: 'from-green-100 to-green-50',
            borderColor: 'border-green-300'
        },
        { 
            id: 'calm', 
            icon: '🌊', 
            label: '寻求平静',
            color: 'from-blue-100 to-blue-50',
            borderColor: 'border-blue-300'
        },
        { 
            id: 'stress', 
            icon: '🌪️', 
            label: '压力过载',
            color: 'from-orange-100 to-orange-50',
            borderColor: 'border-orange-300'
        },
        { 
            id: 'tired', 
            icon: '🌑', 
            label: '感到疲惫',
            color: 'from-gray-100 to-gray-50',
            borderColor: 'border-gray-300'
        },
        { 
            id: 'explore', 
            icon: '✨', 
            label: '探索自我',
            color: 'from-purple-100 to-purple-50',
            borderColor: 'border-purple-300'
        }
    ];

    // 步骤2：场景定义选项
    const reasonOptions = [
        { id: 'academic', label: '学业与考试压力', icon: '📚' },
        { id: 'future', label: '未来的不确定性', icon: '🔮' },
        { id: 'social', label: '社交或情绪内耗', icon: '💭' },
        { id: 'sleep', label: '改善睡眠质量', icon: '😴' },
        { id: 'anxiety', label: '焦虑与不安', icon: '😰' },
        { id: 'energy', label: '缺乏动力与能量', icon: '🔋' }
    ];

    // 步骤3：疗愈目标选项
    const goalOptions = [
        { id: 'peace', label: '一刻钟的宁静', icon: '🕊️', desc: '短暂的放松与平静' },
        { id: 'focus', label: '专注的能量', icon: '⚡', desc: '恢复清晰的头脑' },
        { id: 'sleep', label: '深度放松入眠', icon: '🌙', desc: '准备进入优质睡眠' },
        { id: 'healing', label: '内心的疗愈', icon: '💚', desc: '处理情绪创伤' }
    ];

    const handleMoodSelect = (moodId) => {
        setSelectedMood(moodId);
        // 添加微动效后自动进入下一步
        setTimeout(() => setStep(2), 600);
    };

    const toggleReason = (reasonId) => {
        setSelectedReasons(prev => 
            prev.includes(reasonId)
                ? prev.filter(id => id !== reasonId)
                : [...prev, reasonId]
        );
    };

    const handleGoalSelect = (goalId) => {
        setSelectedGoal(goalId);
    };

    const handleComplete = () => {
        // 保存用户选择到localStorage
        const userData = {
            nickname: nickname,
            gender: gender,
            birthday: birthday,
            mood: selectedMood,
            reasons: selectedReasons,
            goal: selectedGoal,
            completedAt: new Date().toISOString()
        };
        localStorage.setItem('mindnest_welcome_data', JSON.stringify(userData));
        localStorage.setItem('mindnest_welcome_completed', 'true');

        // 通知父组件完成
        if (onComplete) {
            onComplete(userData);
        }
    };

    // 渲染步骤0：基本信息收集
    const renderStep0 = () => (
        <div className="flex flex-col items-center justify-center min-h-screen bg-gradient-to-b from-cream to-white p-6 animate-fade-in">
            {/* Logo/标题区 */}
            <div className="text-center mb-12">
                <h1 className="text-4xl font-bold text-sage-green mb-3">MindNest</h1>
                <div className="w-20 h-1 bg-sage-green mx-auto rounded-full"></div>
                <p className="text-sm text-gray-500 mt-4">你的心灵栖息地</p>
            </div>

            {/* 主文案 */}
            <div className="text-center mb-10">
                <h2 className="text-2xl font-medium text-gray-800 mb-3">
                    让我们先认识一下吧
                </h2>
                <p className="text-base text-gray-600">
                    告诉我一些关于你的小秘密 ✨
                </p>
            </div>

            {/* 表单区 */}
            <div className="w-full max-w-md space-y-6">
                {/* 昵称输入 */}
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        我可以怎么称呼你？
                    </label>
                    <input
                        type="text"
                        value={nickname}
                        onChange={(e) => setNickname(e.target.value)}
                        placeholder="给自己起个可爱的昵称吧"
                        className="w-full px-4 py-3 rounded-xl border-2 border-gray-200 focus:border-sage-green focus:outline-none transition-colors text-base"
                        maxLength={20}
                    />
                    <p className="text-xs text-gray-500 mt-1">
                        {nickname.length}/20
                    </p>
                </div>

                {/* 性别选择 */}
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-3">
                        你的性别是？
                    </label>
                    <div className="grid grid-cols-2 gap-3">
                        {genderOptions.map((option) => (
                            <button
                                key={option.id}
                                onClick={() => setGender(option.id)}
                                className={`
                                    p-4 rounded-xl border-2 transition-all duration-200
                                    bg-gradient-to-br ${option.color}
                                    ${gender === option.id
                                        ? 'border-sage-green ring-2 ring-sage-green ring-opacity-50 scale-105'
                                        : 'border-gray-200 hover:border-sage-green'
                                    }
                                `}
                            >
                                <div className="text-3xl mb-2">{option.icon}</div>
                                <div className="text-sm font-medium text-gray-700">
                                    {option.label}
                                </div>
                            </button>
                        ))}
                    </div>
                </div>

                {/* 生日输入 */}
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        你的生日是？
                    </label>
                    <input
                        type="date"
                        value={birthday}
                        onChange={(e) => setBirthday(e.target.value)}
                        max={new Date().toISOString().split('T')[0]}
                        className="w-full px-4 py-3 rounded-xl border-2 border-gray-200 focus:border-sage-green focus:outline-none transition-colors text-base"
                    />
                    <p className="text-xs text-gray-500 mt-1">
                        我们会在特殊的日子给你惊喜 🎂
                    </p>
                </div>
            </div>

            {/* 继续按钮 */}
            <button
                onClick={() => setStep(1)}
                disabled={!nickname.trim() || !gender}
                className={`
                    w-full max-w-md mt-8 py-4 rounded-xl font-semibold text-lg transition-all duration-300
                    ${nickname.trim() && gender
                        ? 'bg-sage-green text-white hover:bg-opacity-90 shadow-lg hover:shadow-xl'
                        : 'bg-gray-200 text-gray-400 cursor-not-allowed'
                    }
                `}
            >
                开始我的旅程 →
            </button>

            {/* 隐私提示 */}
            <p className="text-xs text-gray-400 mt-6 text-center max-w-xs">
                🔒 你的信息将被安全保护，仅用于个性化体验
            </p>

            {/* 进度指示器 */}
            <div className="flex gap-2 mt-8">
                <div className="w-8 h-2 bg-sage-green rounded-full"></div>
                <div className="w-2 h-2 bg-gray-300 rounded-full"></div>
                <div className="w-2 h-2 bg-gray-300 rounded-full"></div>
                <div className="w-2 h-2 bg-gray-300 rounded-full"></div>
            </div>
        </div>
    );

    // 渲染步骤1：情感切入
    const renderStep1 = () => (
        <div className="flex flex-col items-center justify-center min-h-screen bg-gradient-to-b from-cream to-white p-6 animate-fade-in">
            {/* Logo/标题区 */}
            <div className="text-center mb-12">
                <h1 className="text-3xl font-bold text-sage-green mb-3">MindNest</h1>
                <div className="w-16 h-1 bg-sage-green mx-auto rounded-full"></div>
            </div>

            {/* 主文案 */}
            <div className="text-center mb-12">
                <h2 className="text-2xl font-medium text-gray-800 mb-3">
                    很高兴在 MindNest 遇见你
                </h2>
                <p className="text-lg text-gray-600">
                    今天，你想在这里存放什么样的心情？
                </p>
            </div>

            {/* 心情选项 */}
            <div className="grid grid-cols-2 gap-4 w-full max-w-md">
                {moodOptions.map((mood) => (
                    <button
                        key={mood.id}
                        onClick={() => handleMoodSelect(mood.id)}
                        className={`
                            p-6 rounded-2xl border-2 transition-all duration-300
                            bg-gradient-to-br ${mood.color} ${mood.borderColor}
                            hover:scale-105 hover:shadow-lg
                            active:scale-95
                            ${selectedMood === mood.id ? 'ring-4 ring-sage-green ring-opacity-50 scale-105' : ''}
                        `}
                    >
                        <div className="text-5xl mb-3 animate-bounce-gentle">{mood.icon}</div>
                        <div className="text-base font-medium text-gray-700">{mood.label}</div>
                    </button>
                ))}
            </div>

            {/* 进度指示器 */}
            <div className="flex gap-2 mt-12">
                <div className="w-2 h-2 bg-sage-green rounded-full"></div>
                <div className="w-8 h-2 bg-sage-green rounded-full"></div>
                <div className="w-2 h-2 bg-gray-300 rounded-full"></div>
                <div className="w-2 h-2 bg-gray-300 rounded-full"></div>
            </div>
        </div>
    );

    // 渲染步骤2：场景定义
    const renderStep2 = () => (
        <div className="flex flex-col items-center justify-center min-h-screen bg-gradient-to-b from-cream to-white p-6 animate-fade-in">
            {/* 主文案 */}
            <div className="text-center mb-8">
                <h2 className="text-2xl font-medium text-gray-800 mb-3">
                    是什么让你来到了这里？
                </h2>
                <p className="text-base text-gray-600">
                    （可以选择多个）
                </p>
            </div>

            {/* 原因选项 */}
            <div className="w-full max-w-md space-y-3 mb-8">
                {reasonOptions.map((reason) => (
                    <button
                        key={reason.id}
                        onClick={() => toggleReason(reason.id)}
                        className={`
                            w-full p-4 rounded-xl border-2 transition-all duration-200
                            flex items-center gap-4
                            ${selectedReasons.includes(reason.id)
                                ? 'bg-sage-green bg-opacity-10 border-sage-green shadow-md'
                                : 'bg-white border-gray-200 hover:border-sage-green hover:shadow-sm'
                            }
                        `}
                    >
                        <span className="text-3xl">{reason.icon}</span>
                        <span className={`text-base font-medium flex-1 text-left ${
                            selectedReasons.includes(reason.id) ? 'text-sage-green' : 'text-gray-700'
                        }`}>
                            {reason.label}
                        </span>
                        {selectedReasons.includes(reason.id) && (
                            <svg className="w-6 h-6 text-sage-green" fill="currentColor" viewBox="0 0 20 20">
                                <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                            </svg>
                        )}
                    </button>
                ))}
            </div>

            {/* 按钮组 */}
            <div className="w-full max-w-md flex gap-4">
                <button
                    onClick={() => setStep(1)}
                    className="flex-1 py-3 px-6 rounded-xl border-2 border-gray-300 text-gray-600 font-medium hover:bg-gray-50 transition-all"
                >
                    上一步
                </button>
                <button
                    onClick={() => setStep(3)}
                    disabled={selectedReasons.length === 0}
                    className={`
                        flex-1 py-3 px-6 rounded-xl font-medium transition-all
                        ${selectedReasons.length > 0
                            ? 'bg-sage-green text-white hover:bg-opacity-90 shadow-lg'
                            : 'bg-gray-200 text-gray-400 cursor-not-allowed'
                        }
                    `}
                >
                    下一步
                </button>
            </div>

            {/* 进度指示器 */}
            <div className="flex gap-2 mt-8">
                <div className="w-2 h-2 bg-sage-green rounded-full"></div>
                <div className="w-2 h-2 bg-sage-green rounded-full"></div>
                <div className="w-8 h-2 bg-sage-green rounded-full"></div>
                <div className="w-2 h-2 bg-gray-300 rounded-full"></div>
            </div>
        </div>
    );

    // 渲染步骤3：仪式感开启
    const renderStep3 = () => (
        <div className="flex flex-col items-center justify-center min-h-screen bg-gradient-to-b from-cream to-white p-6 animate-fade-in">
            {/* 主文案 */}
            <div className="text-center mb-8">
                <h2 className="text-2xl font-medium text-gray-800 mb-3">
                    通过这次疗愈
                </h2>
                <p className="text-lg text-gray-600">
                    你希望收获什么？
                </p>
            </div>

            {/* 目标选项 */}
            <div className="w-full max-w-md space-y-4 mb-12">
                {goalOptions.map((goal) => (
                    <button
                        key={goal.id}
                        onClick={() => handleGoalSelect(goal.id)}
                        className={`
                            w-full p-6 rounded-2xl border-2 transition-all duration-300
                            ${selectedGoal === goal.id
                                ? 'bg-sage-green bg-opacity-10 border-sage-green shadow-xl scale-105'
                                : 'bg-white border-gray-200 hover:border-sage-green hover:shadow-lg hover:scale-102'
                            }
                        `}
                    >
                        <div className="flex items-start gap-4">
                            <span className="text-4xl">{goal.icon}</span>
                            <div className="flex-1 text-left">
                                <div className={`text-lg font-semibold mb-1 ${
                                    selectedGoal === goal.id ? 'text-sage-green' : 'text-gray-800'
                                }`}>
                                    {goal.label}
                                </div>
                                <div className="text-sm text-gray-600">{goal.desc}</div>
                            </div>
                        </div>
                    </button>
                ))}
            </div>

            {/* 开启按钮 */}
            <button
                onClick={handleComplete}
                disabled={!selectedGoal}
                className={`
                    w-full max-w-md py-5 rounded-2xl font-bold text-lg transition-all duration-300
                    ${selectedGoal
                        ? 'bg-gradient-to-r from-sage-green to-emerald-600 text-white shadow-2xl hover:shadow-sage-green/50 hover:scale-105 active:scale-95'
                        : 'bg-gray-200 text-gray-400 cursor-not-allowed'
                    }
                `}
            >
                <span className="flex items-center justify-center gap-2">
                    <span>🚪</span>
                    <span>开启我的疗愈之门</span>
                </span>
            </button>

            {/* 返回按钮 */}
            <button
                onClick={() => setStep(2)}
                className="mt-6 text-gray-500 hover:text-gray-700 transition-colors"
            >
                ← 返回上一步
            </button>

            {/* 进度指示器 */}
            <div className="flex gap-2 mt-8">
                <div className="w-2 h-2 bg-sage-green rounded-full"></div>
                <div className="w-2 h-2 bg-sage-green rounded-full"></div>
                <div className="w-2 h-2 bg-sage-green rounded-full"></div>
                <div className="w-8 h-2 bg-sage-green rounded-full"></div>
            </div>
        </div>
    );

    // 根据当前步骤渲染对应内容
    return (
        <div className="welcome-container">
            {step === 0 && renderStep0()}
            {step === 1 && renderStep1()}
            {step === 2 && renderStep2()}
            {step === 3 && renderStep3()}
        </div>
    );
};

export default Welcome;

