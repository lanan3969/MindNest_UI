/**
 * MindNest - Plant Component
 * ===========================
 * 
 * 虚拟植物养成页面，显示养料系统和成长状态
 * 
 * 功能：
 * 1. 显示虚拟植物
 * 2. 显示养料库存（阳光、水、肥料）
 * 3. 显示成长进度
 * 4. 疗愈任务提示
 * 
 * 作者: MindNest Team
 * 日期: 2026-01-26
 */

import React, { useState, useEffect } from 'react';
import axios from 'axios';

const API_BASE_URL = '/api/v1';

const Plant = ({ userId }) => {
    const [plantData, setPlantData] = useState({
        sunlight: 0,
        water: 0,
        fertilizer: 0,
        totalNutrients: 0,
        growthLevel: 1,
        growthProgress: 0
    });
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchPlantData();
    }, [userId]);

    const fetchPlantData = async () => {
        try {
            const response = await axios.get(`${API_BASE_URL}/history/${userId}?limit=30`);
            const history = response.data.recent_history || [];

            // 计算养料总和
            let sunlight = 0;
            let water = 0;
            let fertilizer = 0;

            history.forEach(record => {
                const nutrients = record.nutrients || {};
                sunlight += nutrients.sunlight || 0;
                water += nutrients.water || 0;
                fertilizer += nutrients.fertilizer || 0;
            });

            const totalNutrients = sunlight + water + fertilizer;
            const growthLevel = Math.floor(totalNutrients / 100) + 1;
            const growthProgress = (totalNutrients % 100);

            setPlantData({
                sunlight,
                water,
                fertilizer,
                totalNutrients,
                growthLevel: Math.min(growthLevel, 10), // 最高10级
                growthProgress
            });
        } catch (error) {
            console.error('获取植物数据失败:', error);
        } finally {
            setLoading(false);
        }
    };

    const getPlantEmoji = (level) => {
        const plants = ['🌱', '🌿', '🪴', '🌳', '🌲', '🌴', '🌺', '🌸', '🌼', '🌻'];
        return plants[Math.min(level - 1, plants.length - 1)];
    };

    return (
        <div className="min-h-screen bg-cream">
            {/* Header */}
            <header className="bg-sage-green text-white px-6 py-4">
                <h1 className="text-xl font-medium">My Plant</h1>
            </header>

            {loading ? (
                <div className="flex items-center justify-center h-96">
                    <div className="text-gray-500">Loading...</div>
                </div>
            ) : (
                <div className="px-4 pt-6 pb-24 space-y-6">
                    {/* Plant Display */}
                    <div className="bg-white rounded-3xl shadow-sm p-8 text-center">
                        <div className="text-8xl mb-4">
                            {getPlantEmoji(plantData.growthLevel)}
                        </div>
                        <h2 className="text-2xl font-bold text-gray-800 mb-2">
                            Level {plantData.growthLevel}
                        </h2>
                        <div className="text-sm text-gray-600 mb-4">
                            {plantData.growthLevel === 10 ? '已达到最大等级！' : `距离下一级: ${100 - plantData.growthProgress}点养料`}
                        </div>

                        {/* Growth Progress Bar */}
                        {plantData.growthLevel < 10 && (
                            <div className="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
                                <div
                                    className="bg-sage-green h-full transition-all duration-300"
                                    style={{ width: `${plantData.growthProgress}%` }}
                                ></div>
                            </div>
                        )}
                    </div>

                    {/* Nutrient Inventory */}
                    <div className="bg-white rounded-3xl shadow-sm p-6">
                        <h3 className="text-lg font-semibold text-gray-800 mb-4">养料库存</h3>

                        <div className="space-y-3">
                            {/* Sunlight */}
                            <div className="flex items-center justify-between p-4 bg-yellow-50 rounded-xl">
                                <div className="flex items-center space-x-3">
                                    <span className="text-3xl">☀️</span>
                                    <div>
                                        <div className="font-medium text-gray-800">阳光</div>
                                        <div className="text-xs text-gray-500">来自呼吸练习</div>
                                    </div>
                                </div>
                                <div className="text-2xl font-bold text-yellow-600">
                                    {plantData.sunlight}
                                </div>
                            </div>

                            {/* Water */}
                            <div className="flex items-center justify-between p-4 bg-blue-50 rounded-xl">
                                <div className="flex items-center space-x-3">
                                    <span className="text-3xl">💧</span>
                                    <div>
                                        <div className="font-medium text-gray-800">水分</div>
                                        <div className="text-xs text-gray-500">来自利他行为</div>
                                    </div>
                                </div>
                                <div className="text-2xl font-bold text-blue-600">
                                    {plantData.water}
                                </div>
                            </div>

                            {/* Fertilizer */}
                            <div className="flex items-center justify-between p-4 bg-green-50 rounded-xl">
                                <div className="flex items-center space-x-3">
                                    <span className="text-3xl">🌱</span>
                                    <div>
                                        <div className="font-medium text-gray-800">肥料</div>
                                        <div className="text-xs text-gray-500">来自行为激活</div>
                                    </div>
                                </div>
                                <div className="text-2xl font-bold text-green-600">
                                    {plantData.fertilizer}
                                </div>
                            </div>
                        </div>

                        {/* Total */}
                        <div className="mt-4 pt-4 border-t border-gray-200">
                            <div className="flex items-center justify-between">
                                <span className="text-gray-600 font-medium">总养料</span>
                                <span className="text-3xl font-bold text-sage-green">
                                    {plantData.totalNutrients}
                                </span>
                            </div>
                        </div>
                    </div>

                    {/* Tips */}
                    <div className="bg-gradient-to-r from-sage-green/10 to-blue-gray/10 rounded-3xl p-6">
                        <h3 className="text-lg font-semibold text-gray-800 mb-2">💡 成长秘诀</h3>
                        <ul className="text-sm text-gray-600 space-y-2">
                            <li className="flex items-start">
                                <span className="mr-2">•</span>
                                <span>坚持每天记录心情日记</span>
                            </li>
                            <li className="flex items-start">
                                <span className="mr-2">•</span>
                                <span>完成疗愈任务获得更多养料</span>
                            </li>
                            <li className="flex items-start">
                                <span className="mr-2">•</span>
                                <span>与 Nomi 进行情感互动</span>
                            </li>
                            <li className="flex items-start">
                                <span className="mr-2">•</span>
                                <span>每 100 点养料提升 1 级</span>
                            </li>
                        </ul>
                    </div>
                </div>
            )}
        </div>
    );
};

export default Plant;
