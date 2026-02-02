/**
 * MindNest - Diary Record Component
 * ==================================
 * 
 * 日记记录页面，完美还原用户记录.png的设计
 * 
 * 功能：
 * 1. 心情表情选择器（24个Nomi表情）
 * 2. 主题和内容输入
 * 3. 调用后端API保存
 * 4. 显示Nomi实时反馈
 * 5. 历史记录列表
 * 
 * 作者: MindNest Team
 * 日期: 2026-01-26
 */

import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { getNomiEmojiUrl, getNomiEmojiFallback, preloadNomiExpressions } from '../utils/assetHelper';

// 使用proxy配置，无需完整URL
const API_BASE_URL = '/api/v1';

const DiaryRecord = ({ userId }) => {
    const [diaryContent, setDiaryContent] = useState('');
    const [diarySubject, setDiarySubject] = useState('');
    const [selectedMood, setSelectedMood] = useState(null);
    const [loading, setLoading] = useState(false);
    const [showNomiFeedback, setShowNomiFeedback] = useState(false);
    const [nomiFeedback, setNomiFeedback] = useState(null);
    const [historyRecords, setHistoryRecords] = useState([]);

    // 5个基础心情选项（对应UI截图）
    const moodOptions = [
        { emoji: '😞', label: 'sad', value: 1 },
        { emoji: '😕', label: 'worried', value: 2 },
        { emoji: '😐', label: 'neutral', value: 3 },
        { emoji: '😊', label: 'happy', value: 4 },
        { emoji: '😄', label: 'excited', value: 5 }
    ];

    // 加载历史记录
    useEffect(() => {
        // 预加载Nomi表情
        preloadNomiExpressions();
        fetchHistory();
    }, [userId]);

    const fetchHistory = async () => {
        try {
            const response = await axios.get(`${API_BASE_URL}/history/${userId}?limit=5`);
            setHistoryRecords(response.data.recent_history || []);
        } catch (error) {
            console.error('获取历史记录失败:', error);
        }
    };

    const handleSave = async () => {
        if (!diaryContent.trim()) {
            alert('请输入日记内容');
            return;
        }

        setLoading(true);

        // 准备发送的数据
        const requestData = {
            user_id: userId,
            diary_text: diaryContent,
            conversation_text: `我的主题是：${diarySubject || '无主题'}。心情：${selectedMood ? moodOptions.find(m => m.value === selectedMood)?.label : '未选择'}`,
            timestamp: new Date().toISOString()
        };

        console.log('🚀 正在向后端大脑发送情绪数据...');
        console.log('📝 请求数据:', requestData);
        console.log('🔗 API 端点:', `${API_BASE_URL}/assess`);

        try {
            const response = await axios.post(`${API_BASE_URL}/assess`, requestData);

            console.log('✅ AI 评估结果已回传:', response.data);
            console.log('  📊 焦虑分值:', response.data.anxiety_score);
            console.log('  😊 Nomi 表情:', response.data.nomi_expression);
            console.log('  🌱 养料奖励:', response.data.nutrients);

            // 显示Nomi反馈
            setNomiFeedback(response.data);
            setShowNomiFeedback(true);

            // 清空表单
            setDiaryContent('');
            setDiarySubject('');
            setSelectedMood(null);

            // 刷新历史记录
            fetchHistory();

            // 5秒后自动关闭反馈
            setTimeout(() => setShowNomiFeedback(false), 5000);
        } catch (error) {
            console.error('❌ 保存失败:', error);
            console.error('  错误详情:', error.response?.data || error.message);
            alert(`保存失败: ${error.response?.data?.detail || error.message || '请重试'}`);
        } finally {
            setLoading(false);
        }
    };

    const formatDate = (dateString) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            weekday: 'long'
        });
    };

    return (
        <div className="min-h-screen bg-cream">
            {/* Header */}
            <header className="bg-sage-green text-white px-6 py-4 flex items-center justify-between">
                <h1 className="text-xl font-medium">Record</h1>
                <button className="p-1">
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                </button>
            </header>

            {/* Diary Form */}
            <div className="px-4 pt-4 pb-6">
                <div className="bg-white rounded-3xl shadow-sm p-6 space-y-4">
                    {/* Date */}
                    <div className="text-sm text-gray-500">
                        {new Date().toLocaleDateString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit' })} · {new Date().toLocaleDateString('zh-CN', { weekday: 'long' })}
                    </div>

                    {/* Mood Selector */}
                    <div>
                        <p className="text-sm text-gray-600 mb-2">How are you feeling today?</p>
                        <div className="flex justify-between items-center">
                            {moodOptions.map((mood) => (
                                <button
                                    key={mood.value}
                                    onClick={() => setSelectedMood(mood.value)}
                                    className={`text-3xl transition-transform ${selectedMood === mood.value ? 'scale-125' : 'opacity-60 hover:opacity-100'
                                        }`}
                                >
                                    {mood.emoji}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Subject */}
                    <div>
                        <label className="text-sm text-gray-600 block mb-2">Subject：</label>
                        <input
                            type="text"
                            value={diarySubject}
                            onChange={(e) => setDiarySubject(e.target.value)}
                            placeholder="Give us what happened today..."
                            className="w-full px-4 py-2 text-sm border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-sage-green/50"
                        />
                    </div>

                    {/* Content */}
                    <div>
                        <label className="text-sm text-gray-600 block mb-2">Content：</label>
                        <textarea
                            value={diaryContent}
                            onChange={(e) => setDiaryContent(e.target.value)}
                            placeholder="I Did anything interesting happen today?&#10;Or is there anything that makes you sad?"
                            rows={4}
                            className="w-full px-4 py-3 text-sm border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-sage-green/50 resize-none"
                        />
                    </div>

                    {/* Save Button */}
                    <div className="flex items-center space-x-3">
                        <button
                            onClick={handleSave}
                            disabled={loading}
                            className="flex-1 bg-blue-gray text-white py-3 rounded-xl font-medium hover:bg-blue-gray/90 transition-colors disabled:opacity-50 flex items-center justify-center space-x-2"
                        >
                            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4" />
                            </svg>
                            <span>{loading ? 'Saving...' : 'Save'}</span>
                        </button>
                        <button className="p-3 bg-pink-red text-white rounded-xl hover:bg-pink-red/90 transition-colors">
                            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    </div>
                </div>
            </div>

            {/* History Records */}
            <div className="px-4 pb-6 space-y-3">
                {historyRecords.map((record) => (
                    <div key={record.id} className="bg-white rounded-3xl shadow-sm p-5">
                        <div className="flex items-start justify-between mb-2">
                            <div className="text-xs text-gray-500">
                                {formatDate(record.created_at)}
                            </div>
                            <div className="flex items-center space-x-2">
                                {/* Emotion indicators */}
                                <span className="text-xl">😊</span>
                                <span className="text-xl">❤️</span>
                                <button className="text-gray-400 hover:text-gray-600">
                                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 12h.01M12 12h.01M19 12h.01M6 12a1 1 0 11-2 0 1 1 0 012 0zm7 0a1 1 0 11-2 0 1 1 0 012 0zm7 0a1 1 0 11-2 0 1 1 0 012 0z" />
                                    </svg>
                                </button>
                            </div>
                        </div>
                        <div className="text-sm text-gray-800 leading-relaxed">
                            <strong className="font-semibold block mb-1">{record.anxiety_level === 'severe' ? 'A stressful day' : record.anxiety_level === 'moderate' ? 'Mixed feelings' : 'A good day'}</strong>
                            <p className="text-gray-600">
                                焦虑分值: {record.anxiety_score.toFixed(1)} |
                                疗愈方案: {record.healing_suite.join(' + ')}
                            </p>
                        </div>
                    </div>
                ))}
            </div>

            {/* Nomi Feedback Modal */}
            {showNomiFeedback && nomiFeedback && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-6 z-50 animate-fade-in">
                    <div className="bg-white rounded-3xl p-8 max-w-sm w-full text-center space-y-4 animate-slide-up">
                        {/* Nomi Expression */}
                        <div className="w-32 h-32 mx-auto bg-cream rounded-full flex items-center justify-center overflow-hidden">
                            <img
                                src={getNomiEmojiUrl(nomiFeedback.nomi_expression)}
                                alt={nomiFeedback.nomi_expression}
                                className="w-28 h-28 object-contain"
                                onError={(e) => {
                                    // 如果图片加载失败，显示emoji备用方案
                                    e.target.style.display = 'none';
                                    e.target.nextSibling.style.display = 'block';
                                }}
                            />
                            <span
                                className="text-6xl"
                                style={{ display: 'none' }}
                            >
                                {getNomiEmojiFallback(nomiFeedback.nomi_expression)}
                            </span>
                        </div>

                        {/* Message */}
                        <div className="space-y-2">
                            <p className="text-lg font-medium text-gray-800">
                                {nomiFeedback.message}
                            </p>
                            <p className="text-sm text-gray-600">
                                焦虑分值: {nomiFeedback.anxiety_score.toFixed(1)} / 10
                            </p>
                            <div className="text-xs text-gray-500">
                                疗愈方案: {nomiFeedback.healing_suite.join(' → ')}
                            </div>
                            <div className="text-xs text-sage-green font-medium">
                                获得养料: {nomiFeedback.total_nutrients} 点
                            </div>
                        </div>

                        {/* Close Button */}
                        <button
                            onClick={() => setShowNomiFeedback(false)}
                            className="w-full bg-sage-green text-white py-3 rounded-xl font-medium hover:bg-sage-green/90 transition-colors"
                        >
                            知道了
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default DiaryRecord;
