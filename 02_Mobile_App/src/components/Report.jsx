/**
 * MindNest - Report Component
 * ============================
 * 
 * 报告页面，还原历史记录.png的设计
 * 
 * 功能：
 * 1. 焦虑水平趋势图表
 * 2. 统计数据展示
 * 3. 成就卡片
 * 
 * 作者: MindNest Team
 * 日期: 2026-01-26
 */

import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { Line } from 'react-chartjs-2';
import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    Title,
    Tooltip,
    Legend,
    Filler
} from 'chart.js';

// Register ChartJS components
ChartJS.register(
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    Title,
    Tooltip,
    Legend,
    Filler
);

// 使用proxy配置
const API_BASE_URL = '/api/v1';

const Report = ({ userId }) => {
    const [historyData, setHistoryData] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchReport();
    }, [userId]);

    const fetchReport = async () => {
        try {
            const response = await axios.get(`${API_BASE_URL}/history/${userId}?limit=14`);
            setHistoryData(response.data);
        } catch (error) {
            console.error('获取报告数据失败:', error);
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-screen bg-cream">
                <div className="text-sage-green">Loading...</div>
            </div>
        );
    }

    // Chart data
    const chartData = {
        labels: historyData?.recent_history?.reverse().map((record, idx) => `Day ${idx + 1}`) || [],
        datasets: [
            {
                label: 'Before Healing',
                data: historyData?.recent_history?.map(r => r.anxiety_score) || [],
                borderColor: 'rgba(255, 182, 193, 1)',
                backgroundColor: 'rgba(255, 182, 193, 0.2)',
                fill: true,
                tension: 0.4,
                pointRadius: 4,
                pointHoverRadius: 6
            },
            {
                label: 'After Healing',
                data: historyData?.recent_history?.map(r => Math.max(0, r.anxiety_score - 1.5)) || [],
                borderColor: 'rgba(168, 181, 165, 1)',
                backgroundColor: 'rgba(168, 181, 165, 0.2)',
                fill: true,
                tension: 0.4,
                pointRadius: 4,
                pointHoverRadius: 6
            }
        ]
    };

    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: true,
                position: 'bottom',
                labels: {
                    usePointStyle: true,
                    padding: 15,
                    font: {
                        size: 11
                    }
                }
            },
            tooltip: {
                backgroundColor: 'rgba(255, 255, 255, 0.9)',
                titleColor: '#333',
                bodyColor: '#666',
                borderColor: '#ddd',
                borderWidth: 1,
                padding: 10,
                displayColors: true
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                max: 10,
                grid: {
                    color: 'rgba(0, 0, 0, 0.05)'
                },
                ticks: {
                    font: {
                        size: 10
                    }
                }
            },
            x: {
                grid: {
                    display: false
                },
                ticks: {
                    font: {
                        size: 10
                    }
                }
            }
        }
    };

    const stats = historyData?.trend_summary || {};

    return (
        <div className="min-h-screen bg-cream pb-20">
            {/* Header */}
            <header className="bg-sage-green text-white px-6 py-4 flex items-center justify-between">
                <h1 className="text-xl font-medium">Report</h1>
                <button className="p-1">
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                    </svg>
                </button>
            </header>

            <div className="px-4 pt-4 space-y-4">
                {/* Achievement Cards */}
                <div className="flex space-x-3">
                    <div className="flex-1 bg-gradient-to-br from-yellow-50 to-orange-50 rounded-2xl p-4 shadow-sm">
                        <div className="text-2xl mb-1">🌬️</div>
                        <div className="text-xs text-gray-600">Longest</div>
                        <div className="text-sm font-semibold">breathing practice</div>
                        <div className="text-lg font-bold text-orange-600 mt-1">New Record</div>
                    </div>
                    <div className="flex-1 bg-gradient-to-br from-pink-50 to-red-50 rounded-2xl p-4 shadow-sm">
                        <div className="text-2xl mb-1">🎅</div>
                        <div className="text-xs text-gray-600">2025 new style!</div>
                        <div className="text-sm font-semibold">Get Christmas hat</div>
                    </div>
                </div>

                {/* Trend Chart */}
                <div className="bg-white rounded-3xl p-5 shadow-sm">
                    <h3 className="text-base font-semibold text-gray-800 mb-4">
                        Trend in Anxiety Level Changes
                    </h3>
                    <div className="h-48">
                        <Line data={chartData} options={chartOptions} />
                    </div>
                </div>

                {/* Anxiety Pattern Insights */}
                <div className="bg-white rounded-3xl p-5 shadow-sm">
                    <h3 className="text-base font-semibold text-gray-800 mb-4">
                        Anxiety Pattern Insights
                    </h3>
                    <div className="grid grid-cols-2 gap-3">
                        <div className="bg-cream rounded-xl p-3">
                            <div className="flex items-center space-x-2 mb-2">
                                <div className="w-8 h-8 bg-orange-100 rounded-full flex items-center justify-center text-orange-600">
                                    🕐
                                </div>
                                <div>
                                    <div className="text-xs text-gray-600">Duration of</div>
                                    <div className="text-xs font-semibold">falling asleep</div>
                                </div>
                            </div>
                            <div className="text-lg font-bold">120min <span className="text-xs text-red-500">(+15%)</span></div>
                        </div>

                        <div className="bg-cream rounded-xl p-3">
                            <div className="flex items-center space-x-2 mb-2">
                                <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-blue-600">
                                    😴
                                </div>
                                <div>
                                    <div className="text-xs text-gray-600">Duration of</div>
                                    <div className="text-xs font-semibold">deep sleep</div>
                                </div>
                            </div>
                            <div className="text-lg font-bold">20min <span className="text-xs text-green-500">(-5min)</span></div>
                        </div>

                        <div className="bg-cream rounded-xl p-3">
                            <div className="flex items-center space-x-2 mb-2">
                                <div className="w-8 h-8 bg-yellow-100 rounded-full flex items-center justify-center text-yellow-600">
                                    ☀️
                                </div>
                                <div>
                                    <div className="text-xs text-gray-600">Mood record</div>
                                </div>
                            </div>
                            <div className="text-lg font-bold">14 times</div>
                        </div>

                        <div className="bg-cream rounded-xl p-3">
                            <div className="flex items-center space-x-2 mb-2">
                                <div className="w-8 h-8 bg-purple-100 rounded-full flex items-center justify-center text-purple-600">
                                    💬
                                </div>
                                <div>
                                    <div className="text-xs text-gray-600">Number of</div>
                                    <div className="text-xs font-semibold">interactions</div>
                                </div>
                            </div>
                            <div className="text-lg font-bold">10 <span className="text-xs text-green-500">(+2)</span></div>
                        </div>

                        <div className="bg-cream rounded-xl p-3">
                            <div className="flex items-center space-x-2 mb-2">
                                <div className="w-8 h-8 bg-indigo-100 rounded-full flex items-center justify-center text-indigo-600">
                                    🌙
                                </div>
                                <div>
                                    <div className="text-xs text-gray-600">Sleep quality</div>
                                </div>
                            </div>
                            <div className="text-lg font-bold">7.5h <span className="text-xs text-gray-500">(+0.5h)</span></div>
                        </div>

                        <div className="bg-cream rounded-xl p-3">
                            <div className="flex items-center space-x-2 mb-2">
                                <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center text-green-600">
                                    ⭐
                                </div>
                                <div>
                                    <div className="text-xs text-gray-600">Else</div>
                                </div>
                            </div>
                            <div className="text-lg font-bold">...</div>
                        </div>
                    </div>
                </div>

                {/* Share Button */}
                <button className="w-full bg-pink-red text-white py-4 rounded-2xl font-medium hover:bg-pink-red/90 transition-colors flex items-center justify-center space-x-2 shadow-sm">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z" />
                    </svg>
                    <span>Share Report</span>
                </button>

                {/* Statistics Summary */}
                <div className="bg-white/50 rounded-2xl p-4 text-sm text-gray-600">
                    <div className="flex justify-between mb-2">
                        <span>平均焦虑分值:</span>
                        <span className="font-semibold">{stats.average_score || 0}</span>
                    </div>
                    <div className="flex justify-between mb-2">
                        <span>趋势:</span>
                        <span className={`font-semibold ${stats.trend === 'improving' ? 'text-green-600' :
                            stats.trend === 'worsening' ? 'text-red-600' :
                                'text-gray-600'
                            }`}>
                            {stats.trend === 'improving' ? '📉 改善中' :
                                stats.trend === 'worsening' ? '📈 需关注' :
                                    '➡️ 稳定'}
                        </span>
                    </div>
                    <div className="flex justify-between">
                        <span>记录次数:</span>
                        <span className="font-semibold">{historyData?.total_records || 0} 次</span>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Report;
