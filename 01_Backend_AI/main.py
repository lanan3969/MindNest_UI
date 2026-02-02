"""
MindNest Backend API
====================

FastAPI服务，提供焦虑评估、分级疗愈推荐、情绪表情映射等功能。

核心功能：
1. 双源输入评估（日记 + 对话）
2. 调用 ModelScope Qwen-2.5 进行情感分析
3. 分级疗愈逻辑（轻度/中度/重度）
4. Nomi 24表情映射（深度情绪识别）
5. 养料系统管理
6. SQLite数据持久化
7. 历史记录与趋势分析

作者: MindNest Team
日期: 2026-01-26
"""

from fastapi import FastAPI, HTTPException, status, Depends
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel, Field, ConfigDict
from typing import Optional, List, Dict
from datetime import datetime
from contextlib import asynccontextmanager
import json
import random
import os
from http import HTTPStatus
from sqlalchemy.orm import Session

# 绕过代理直连 ModelScope API
os.environ["NO_PROXY"] = "modelscope.cn,api.modelscope.cn"
os.environ["no_proxy"] = "modelscope.cn,api.modelscope.cn"

# 导入自定义模块
from database import (
    init_db, get_db, save_assessment, get_user_history,
    get_assessment_stats, assessment_to_dict, AssessmentHistory
)
from emotion_mapping import (
    get_nomi_expression, ENHANCED_SYSTEM_PROMPT, get_all_expressions
)

# ============================================================================
# 配置部分
# ============================================================================

# 加载.env文件中的环境变量
from dotenv import load_dotenv
load_dotenv()

# ModelScope API配置（需要设置环境变量 MODELSCOPE_API_KEY）
MODELSCOPE_API_KEY = os.getenv("MODELSCOPE_API_KEY", "")

# 🔧 启动时显示 API Key 验证
print(f"\n{'='*70}")
print(f"🔑 API Key 配置验证")
print(f"{'='*70}")
if MODELSCOPE_API_KEY:
    print(f"✅ API Key 已加载")
    print(f"   前8位: {MODELSCOPE_API_KEY[:8]}...")
    print(f"   长度: {len(MODELSCOPE_API_KEY)} 字符")
else:
    print(f"❌ API Key 未配置!")
print(f"{'='*70}\n")

# 是否使用Mock模式（开发测试用，不调用真实API）
# 当 API Key 存在时，使用生产模式（False = 真实API调用）
USE_MOCK_MODE = False if MODELSCOPE_API_KEY else True

# 行为激活任务池（来自 tree_final.html）
TASK_POOL = [
    "整理你的桌面 5 分钟",
    "出门散步 10 分钟",
    "给一位朋友发一条消息",
    "听一首从未听过的新歌",
    "尝试一个新食谱",
    "进行一项创意活动（绘画、写作等）",
    "练习正念冥想 5 分钟",
    "主动联系一位朋友",
    "为你关心的事业做志愿者",
    "加入一个兴趣小组",
    "参观当地博物馆或公园",
    "和朋友一起看电影/剧集",
    "慢跑或快走 15 分钟",
    "去户外接触自然",
    "尝试一项新运动",
    "骑自行车或滑轮滑",
    "做温和的拉伸或瑜伽",
    "清理一个抽屉或柜子",
    "每天早上叠被子",
    "洗个舒适的热水澡"
]

# Nomi表情映射表（英文文件名，匹配前端 assets）
EMOTION_MAP = {
    (9.0, 10.1): {"file": "cpu_burned.png", "emotion": "extremely_stressed", "description": "极度焦虑"},
    (7.0, 9.0): {"file": "sad.png", "emotion": "sad", "description": "悲伤难过"},
    (5.0, 7.0): {"file": "question.png", "emotion": "worried", "description": "困惑担忧"},
    (3.5, 5.0): {"file": "thinking.png", "emotion": "neutral", "description": "思考中立"},
    (0.0, 3.5): {"file": "happy.png", "emotion": "happy", "description": "快乐平静"}
}

# 养料类型配置（叠加式）
NUTRIENT_CONFIG = {
    "breathing": {"type": "sunlight", "emoji": "☀️", "amount": 10, "description": "阳光"},
    "altruistic": {"type": "water", "emoji": "💧", "amount": 15, "description": "水"},
    "behavioral_activation": {"type": "fertilizer", "emoji": "🌱", "amount": 25, "description": "肥料"}
}

# 叠加式疗愈配置
HEALING_SUITE_CONFIG = {
    "light": ["breathing"],  # 轻度：仅呼吸
    "moderate": ["breathing", "altruistic"],  # 中度：呼吸 + 利他
    "severe": ["breathing", "altruistic", "behavioral_activation"]  # 重度：全部叠加
}

# ============================================================================
# Pydantic数据模型
# ============================================================================

class AssessmentRequest(BaseModel):
    """评估请求模型"""
    model_config = ConfigDict(json_schema_extra={
        "example": {
            "user_id": "user_12345",
            "diary_text": "今天考试考砸了，感觉很失落，晚上也睡不好...",
            "conversation_text": "Nomi，我觉得自己很失败，不知道该怎么办",
            "timestamp": "2026-01-26T10:00:00Z"
        }
    })
    
    user_id: str = Field(..., description="用户唯一标识")
    diary_text: str = Field(..., min_length=1, description="最近一篇心情日记内容")
    conversation_text: str = Field(..., min_length=1, description="与Nomi的实时对话内容")
    timestamp: Optional[str] = Field(default=None, description="评估时间戳（ISO 8601格式）")


class AssessmentResponse(BaseModel):
    """评估响应模型（叠加式）"""
    model_config = ConfigDict(json_schema_extra={
        "example": {
            "anxiety_score": 6.2,
            "anxiety_level": "moderate",
            "healing_path": "moderate",
            "healing_suite": ["breathing", "altruistic"],
            "nutrients": {
                "sunlight": 10,
                "water": 15
            },
            "total_nutrients": 25,
            "nomi_expression": "疑问.png",
            "nomi_emotion": "worried",
            "nomi_state": "worried",
            "task": None,
            "sequence": ["breathing_first", "then_altruistic"],
            "message": "先深呼吸放松，然后去安慰一下 Nomi 吧~ ☀️💧",
            "ai_reasoning": "检测到中度焦虑关键词：2个，存在情绪波动",
            "timestamp": "2026-01-26T10:00:00Z"
        }
    })
    
    anxiety_score: float = Field(..., ge=0.0, le=10.0, description="焦虑分值 [0-10]")
    anxiety_level: str = Field(..., description="焦虑等级：light/moderate/severe")
    healing_path: str = Field(..., description="疗愈路径：light/moderate/severe (与anxiety_level一致)")
    healing_suite: List[str] = Field(..., description="疗愈组合列表（叠加式）")
    nutrients: Dict[str, int] = Field(..., description="养料字典 {类型: 数量}")
    total_nutrients: int = Field(..., description="养料总量")
    nomi_expression: str = Field(..., description="Nomi表情文件名")
    nomi_emotion: str = Field(..., description="情绪标签")
    nomi_state: Optional[str] = Field(default=None, description="Nomi状态：normal/worried")
    task: Optional[str] = Field(default=None, description="离线任务（仅重度焦虑）")
    sequence: List[str] = Field(..., description="疗愈执行顺序")
    message: str = Field(..., description="给用户的提示消息")
    ai_reasoning: str = Field(..., description="AI评估理由")
    timestamp: str = Field(..., description="响应时间戳")


class HealthResponse(BaseModel):
    """健康检查响应"""
    status: str
    message: str
    model_mode: str
    timestamp: str


# ============================================================================
# FastAPI应用初始化
# ============================================================================

@asynccontextmanager
async def lifespan(app: FastAPI):
    """应用生命周期管理"""
    # Startup
    init_db()
    print("🌳 MindNest Backend API 已启动")
    print(f"📊 已加载 {len(get_all_expressions())} 个Nomi表情")
    yield
    # Shutdown (如需要可在此处添加清理逻辑)

app = FastAPI(
    title="MindNest Backend API",
    description="沉浸式MR心理疗愈系统后端服务 | AI Hackathon Tour 2026",
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc",
    lifespan=lifespan
)

# CORS中间件配置（允许跨域请求）
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # 生产环境应限制具体域名
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 静态文件配置（提供 Nomi 表情图片）
if os.path.exists("assets"):
    app.mount("/assets", StaticFiles(directory="assets"), name="assets")


# ============================================================================
# 核心功能函数
# ============================================================================

def call_qwen_api(combined_text: str) -> dict:
    """
    调用 ModelScope Qwen-2.5 API (使用 OpenAI SDK)
    
    **官方 SDK 实现**:
    - 使用 openai 库（ModelScope 官方推荐）
    - 推理端点: api-inference.modelscope.cn
    - httpx 强制直连，绕过所有代理
    
    Args:
        combined_text: 组合后的输入文本
        
    Returns:
        dict: AI评估结果或Mock结果（固定 0.001）
    """
    print(f"⏳ 正在调用 Qwen API（OpenAI SDK + 强制直连）...")
    
    try:
        import httpx
        from openai import OpenAI
        
        # 🔧 强制直连：使用 httpx.Client 绕过代理
        http_client = httpx.Client(
            trust_env=False,  # 忽略环境变量
            proxies=None,     # 显式置空代理
            timeout=30.0
        )
        
        # 创建 OpenAI 客户端（ModelScope 官方推荐）
        client = OpenAI(
            base_url='https://api-inference.modelscope.cn/v1',
            api_key=MODELSCOPE_API_KEY,
            http_client=http_client
        )
        
        # 使用增强版提示词
        SYSTEM_PROMPT = ENHANCED_SYSTEM_PROMPT
        
        # 调用 API（非流式）
        response = client.chat.completions.create(
            model='Qwen/Qwen2.5-7B-Instruct',
            messages=[
                {'role': 'system', 'content': SYSTEM_PROMPT},
                {'role': 'user', 'content': combined_text}
            ],
            temperature=0.3,
            stream=False
        )
        
        # 解析响应
        content = response.choices[0].message.content
        
        # 尝试解析 JSON
        try:
            import json
            parsed = json.loads(content)
            print(f"✅ Qwen API 调用成功 | 分值: {parsed.get('anxiety_score', 'N/A')}")
            return parsed
        except json.JSONDecodeError:
            print(f"⚠️ AI 响应非 JSON 格式")
            print(f"   原始响应: {content[:200]}")
            return mock_ai_assessment(combined_text)
            
    except Exception as e:
        error_msg = str(e)
        error_type = type(e).__name__
        
        # 🚨 超级醒目的错误日志
        print("\n")
        print("❌" * 20)
        print("❌" * 20)
        print(f"🚨 Qwen API 调用失败！")
        print(f"错误类型: {error_type}")
        print("❌" * 20)
        
        print(f"\n📄 完整错误信息:")
        print(error_msg)
        
        print("\n" + "❌" * 20)
        print(f"🔄 强制切换到 Mock 模式（分值将为 0.001）")
        print("❌" * 20 + "\n")
        
        return mock_ai_assessment(combined_text)


def mock_ai_assessment(text: str) -> dict:
    """
    Mock AI评估（用于开发测试）
    
    **特殊标识**: 返回固定分值 0.001 用于识别 Mock 模式
    这样您可以立即判断系统是否连接到云端 AI
    
    Args:
        text: 输入文本
        
    Returns:
        dict: 模拟的AI评估结果，分值固定为 0.001
    """
    print(f"🔧 【Mock模式】正在评估（AI云端未连接）")
    print(f"   文本长度: {len(text)} 字符")
    
    # 简单的情绪判断（仅用于日志）
    has_positive = any(word in text for word in ["开心", "快乐", "高兴", "愉快", "不错", "充实", "满意"])
    has_negative = any(word in text for word in ["焦虑", "压力", "难过", "伤心", "担心", "害怕", "痛苦"])
    
    if has_positive:
        emotion_hint = "积极"
    elif has_negative:
        emotion_hint = "消极"
    else:
        emotion_hint = "中性"
    
    print(f"   情绪倾向: {emotion_hint}")
    print(f"   ⚠️  返回固定分值 0.001（Mock 模式标识）")
    
    # 返回特殊分值 0.001 作为 Mock 模式的明确标识
    # 这样您可以立即判断系统是否连接到云端 AI
    return {
        "anxiety_score": 0.001,  # 固定分值，表示这是 Mock 模式
        "reason": f"【Mock模式】AI云端未连接，使用本地规则引擎。检测到{emotion_hint}倾向。",
        "emotion": "neutral"
    }


# 注意：get_nomi_expression 函数已移至 emotion_mapping.py
# 这里保留一个兼容性包装函数
def get_nomi_expression_legacy(score: float) -> dict:
    """
    兼容旧版：仅基于焦虑分值映射表情
    建议使用 emotion_mapping.get_nomi_expression() 获得更准确的结果
    
    Args:
        score: 焦虑分值 [0-10]
        
    Returns:
        dict: 表情信息
    """
    from emotion_mapping import get_emotion_from_anxiety_score
    return get_emotion_from_anxiety_score(score)


def determine_healing_suite(score: float) -> dict:
    """
    根据焦虑分值确定疗愈组合（叠加式）
    
    Args:
        score: 焦虑分值 [0-10]
        
    Returns:
        dict: 疗愈组合配置
    """
    # 确定焦虑等级
    if score <= 3.5:
        level = "light"
        message = "让我们一起做个深呼吸，平复心情吧 ☀️"
        task = None
        nomi_state = None
        sequence = ["breathing"]
    elif 3.5 < score <= 7:
        level = "moderate"
        message = "先深呼吸放松，然后去安慰一下 Nomi 吧~ ☀️💧"
        task = None
        nomi_state = "worried"
        sequence = ["breathing_first", "then_altruistic"]
    else:  # score > 7
        level = "severe"
        task = random.choice(TASK_POOL)
        message = f"深呼吸 → 安慰 Nomi → 完成任务：{task} ☀️💧🌱"
        nomi_state = "worried"
        sequence = ["breathing_first", "then_altruistic", "finally_task"]
    
    # 获取疗愈组合
    healing_suite = HEALING_SUITE_CONFIG[level]
    
    # 计算叠加养料
    nutrients = {}
    total_nutrients = 0
    
    for mode in healing_suite:
        nutrient_info = NUTRIENT_CONFIG[mode]
        nutrient_type = nutrient_info["type"]
        nutrient_amount = nutrient_info["amount"]
        nutrients[nutrient_type] = nutrient_amount
        total_nutrients += nutrient_amount
    
    return {
        "level": level,
        "healing_suite": healing_suite,
        "nutrients": nutrients,
        "total_nutrients": total_nutrients,
        "message": message,
        "task": task,
        "nomi_state": nomi_state,
        "sequence": sequence
    }


# ============================================================================
# API路由
# ============================================================================

@app.get("/", response_model=HealthResponse)
async def root():
    """根路径健康检查"""
    return {
        "status": "healthy",
        "message": "MindNest Backend API is running",
        "model_mode": "Mock Mode" if USE_MOCK_MODE else "Production Mode",
        "timestamp": datetime.now().isoformat()
    }


@app.get("/health", response_model=HealthResponse)
async def health_check():
    """健康检查接口"""
    return {
        "status": "healthy",
        "message": "All systems operational",
        "model_mode": "Mock Mode (开发测试)" if USE_MOCK_MODE else "Production Mode (真实API)",
        "timestamp": datetime.now().isoformat()
    }


@app.post("/api/v1/assess", response_model=AssessmentResponse, status_code=status.HTTP_200_OK)
async def assess_anxiety(request: AssessmentRequest, db: Session = Depends(get_db)):
    """
    核心评估接口
    
    功能：
    1. 接收用户日记和对话文本
    2. 调用 Qwen-2.5 进行情感分析
    3. 计算焦虑分值
    4. 推荐疗愈模式
    5. 映射 Nomi 表情
    6. 生成离线任务（如需要）
    
    Args:
        request: AssessmentRequest 对象
        
    Returns:
        AssessmentResponse: 评估结果
        
    Raises:
        HTTPException: 参数错误或服务异常
    """
    try:
        # 1. 合并双源输入
        combined_text = f"""
【近期情绪记录】
{request.diary_text}

【当前对话内容】
{request.conversation_text}
"""
        
        # 2. 调用 AI 进行评估
        ai_result = call_qwen_api(combined_text)
        anxiety_score = ai_result["anxiety_score"]
        ai_reasoning = ai_result["reason"]
        
        # 3. 确定疗愈组合（叠加式）
        healing_info = determine_healing_suite(anxiety_score)
        
        # 4. 提取 healing_path（优先使用AI返回，否则根据分值判定）
        healing_path = ai_result.get("healing_path", None)
        if not healing_path or healing_path not in ["light", "moderate", "severe"]:
            # Fallback: 根据焦虑分值自动判定
            healing_path = healing_info["level"]
        
        # 5. 映射 Nomi 表情（使用增强版：24表情 + 关键词匹配）
        ai_emotion = ai_result.get("emotion", None)
        expression_info = get_nomi_expression(
            anxiety_score=anxiety_score,
            combined_text=combined_text,
            ai_emotion=ai_emotion
        )
        
        # 6. 构建响应
        response = AssessmentResponse(
            anxiety_score=anxiety_score,
            anxiety_level=healing_info["level"],
            healing_path=healing_path,
            healing_suite=healing_info["healing_suite"],
            nutrients=healing_info["nutrients"],
            total_nutrients=healing_info["total_nutrients"],
            nomi_expression=expression_info["file"],
            nomi_emotion=expression_info["emotion"],
            nomi_state=healing_info["nomi_state"],
            task=healing_info["task"],
            sequence=healing_info["sequence"],
            message=healing_info["message"],
            ai_reasoning=ai_reasoning,
            timestamp=request.timestamp or datetime.now().isoformat()
        )
        
        # 7. 保存到数据库
        save_assessment(
            db=db,
            user_id=request.user_id,
            anxiety_score=anxiety_score,
            anxiety_level=healing_info["level"],
            healing_suite=healing_info["healing_suite"],
            nutrients=healing_info["nutrients"],
            total_nutrients=healing_info["total_nutrients"],
            nomi_expression=expression_info["file"],
            nomi_emotion=expression_info["emotion"],
            nomi_state=healing_info["nomi_state"],
            task=healing_info["task"],
            diary_text=request.diary_text,
            conversation_text=request.conversation_text,
            ai_reasoning=ai_reasoning
        )
        
        # 7. 日志记录
        print(f"✅ 评估完成 | User: {request.user_id} | Score: {anxiety_score} | Level: {healing_info['level']} | Expression: {expression_info['file']} | Nutrients: {healing_info['total_nutrients']}")
        
        return response
        
    except ValueError as ve:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"参数验证失败: {str(ve)}"
        )
    except Exception as e:
        print(f"❌ 评估失败: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"服务内部错误: {str(e)}"
        )


@app.get("/api/v1/tasks")
async def get_tasks():
    """
    获取所有可用的行为激活任务
    
    Returns:
        dict: 任务列表
    """
    return {
        "total": len(TASK_POOL),
        "tasks": TASK_POOL,
        "timestamp": datetime.now().isoformat()
    }


@app.get("/api/v1/expressions")
async def get_expressions():
    """
    获取所有 Nomi 表情映射规则（24个表情）
    
    Returns:
        dict: 表情映射表
    """
    all_expressions = get_all_expressions()
    
    return {
        "total_expressions": len(all_expressions),
        "expression_files": all_expressions,
        "timestamp": datetime.now().isoformat()
    }


@app.get("/api/v1/history/{user_id}")
async def get_history(user_id: str, limit: int = 7, db: Session = Depends(get_db)):
    """
    获取用户评估历史记录
    
    Args:
        user_id: 用户ID
        limit: 返回记录数量（默认7条）
        
    Returns:
        dict: 历史记录和趋势分析
    """
    try:
        # 获取历史记录
        history = get_user_history(db, user_id, limit)
        
        # 获取统计数据
        stats = get_assessment_stats(db, user_id)
        
        # 转换为字典
        history_list = [assessment_to_dict(h) for h in history]
        
        return {
            "user_id": user_id,
            "total_records": stats["total_assessments"],
            "recent_history": history_list,
            "trend_summary": {
                "average_score": stats["average_score"],
                "trend": stats["trend"],
                "lowest_score": stats["lowest_score"],
                "highest_score": stats["highest_score"]
            },
            "timestamp": datetime.now().isoformat()
        }
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"获取历史记录失败: {str(e)}"
        )


@app.get("/api/v1/mr_sync/{user_id}")
async def mr_sync(user_id: str, db: Session = Depends(get_db)):
    """
    MR端数据同步接口
    
    功能：为Unity MR应用提供实时数据同步
    - 返回用户最新一条评估记录
    - 计算该用户的累计养料总额
    
    Args:
        user_id: 用户ID
        
    Returns:
        dict: {
            "score": 焦虑分值,
            "expression": Nomi表情文件名,
            "healing_suggestion": 疗愈建议,
            "total_nutrients": 累计养料总额
        }
        
    Raises:
        HTTPException: 用户不存在或无评估记录
    """
    try:
        # 1. 获取用户最新一条评估记录
        latest_assessment = db.query(AssessmentHistory)\
            .filter(AssessmentHistory.user_id == user_id)\
            .order_by(AssessmentHistory.created_at.desc())\
            .first()
        
        # 2. 检查是否有记录
        if not latest_assessment:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"User {user_id} has no assessment records yet"
            )
        
        # 3. 计算累计养料总额（所有历史记录的总和）
        total_nutrients_sum = db.query(AssessmentHistory)\
            .filter(AssessmentHistory.user_id == user_id)\
            .with_entities(AssessmentHistory.total_nutrients)\
            .all()
        
        # 累加所有养料
        cumulative_nutrients = sum([record[0] for record in total_nutrients_sum if record[0]])
        
        # 4. 构建疗愈建议（基于当前等级）
        healing_suggestion = ""
        if latest_assessment.anxiety_level == "light":
            healing_suggestion = "让我们一起做个深呼吸，平复心情吧 ☀️"
        elif latest_assessment.anxiety_level == "moderate":
            healing_suggestion = "先深呼吸放松，然后去安慰一下 Nomi 吧~ ☀️💧"
        else:  # severe
            task_hint = f"任务: {latest_assessment.task}" if latest_assessment.task else "完成行为激活任务"
            healing_suggestion = f"深呼吸 → 安慰 Nomi → {task_hint} ☀️💧🌱"
        
        # 5. 返回MR端所需数据
        return {
            "score": latest_assessment.anxiety_score,
            "expression": latest_assessment.nomi_expression,
            "healing_suggestion": healing_suggestion,
            "total_nutrients": cumulative_nutrients,
            "anxiety_level": latest_assessment.anxiety_level,
            "timestamp": latest_assessment.created_at.isoformat()
        }
        
    except HTTPException:
        # 重新抛出HTTP异常
        raise
    except Exception as e:
        print(f"❌ MR同步失败: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"MR sync failed: {str(e)}"
        )


# ============================================================================
# 启动提示
# ============================================================================

if __name__ == "__main__":
    import uvicorn
    
    print("=" * 60)
    print("🌳 MindNest Backend API Starting...")
    print("=" * 60)
    print(f"📌 Mode: {'Mock (开发测试)' if USE_MOCK_MODE else 'Production (真实API)'}")
    print(f"📌 API Key 已配置: {'否 ⚠️' if USE_MOCK_MODE else '是 ✅'}")
    print(f"📌 访问文档: http://localhost:8000/docs")
    print("=" * 60)
    
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info"
    )
