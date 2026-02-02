"""
MindNest Emotion Mapping Module
================================

24个Nomi表情的深度情绪映射逻辑

功能：
1. 基于焦虑分值的基础映射
2. 基于关键词的情感识别
3. AI辅助的情绪标签提取

作者: MindNest Team
日期: 2026-01-26
"""

from typing import Dict, List, Optional

# ============================================================================
# 24个Nomi表情完整映射表
# ============================================================================

# 基础情绪映射（基于焦虑分值）
ANXIETY_BASED_EMOTIONS = {
    (9.0, 10.1): {"file": "cpu_burned.png", "emotion": "extremely_stressed", "description": "极度焦虑，CPU过载"},
    (8.0, 9.0): {"file": "sad.png", "emotion": "sad", "description": "悲伤难过，情绪低落"},
    (7.0, 8.0): {"file": "question.png", "emotion": "confused", "description": "困惑担忧，需要支持"},
    (5.5, 7.0): {"file": "thinking.png", "emotion": "thinking", "description": "思考中立，轻微迷茫"},
    (4.0, 5.5): {"file": "ok.png", "emotion": "approval", "description": "还可以，基本稳定"},
    (2.0, 4.0): {"file": "happy.png", "emotion": "happy", "description": "开心愉快，状态良好"},
    (0.0, 2.0): {"file": "celebrate.png", "emotion": "celebrating", "description": "非常开心，值得庆祝"}
}

# 关键词情绪映射（🔧 已清空用于调试验证）
KEYWORD_EMOTION_MAP = {}
# KEYWORD_EMOTION_MAP = {
#     "greeting": {
#         "file": "welcome.png",
#         "keywords": ["你好", "嗨", "打招呼", "早上好", "晚上好", "hi", "hello"],
#         "description": "打招呼"
#     },
#     ... (已注释所有关键词映射
# }

# AI返回的情绪标签与表情文件映射
AI_EMOTION_LABELS = {
    "greeting": "welcome.png",
    "approval": "ok.png",
    "rejection": "no.png",
    "happy": "happy.png",
    "sad": "sad.png",
    "angry": "angry.png",
    "surprised": "surprise.png",
    "celebrating": "celebrate.png",
    "encouraging": "cheer.png",
    "grateful": "thanks.png",
    "thinking": "thinking.png",
    "confused": "question.png",
    "pleading": "please.png",
    "relaxing": "slacking.png",
    "meditating": "meditation.png",
    "sleepy": "goodnight.png",
    "eating": "eating.png",
    "rushing": "deadline.png",
    "lucky": "lucky.png",
    "wealthy": "rich.png",
    "loving": "love.png",
    "approving": "like.png",
    "playful": "naughty.png",
    "extremely_stressed": "cpu_burned.png"
}


# ============================================================================
# 情绪识别函数
# ============================================================================

def detect_emotion_from_keywords(text: str) -> Optional[Dict]:
    """
    基于关键词检测情绪
    
    Args:
        text: 用户输入的文本（日记+对话）
        
    Returns:
        dict: 情绪信息 {"file": "xxx.png", "emotion": "xxx", "description": "xxx"}
              如果未匹配到关键词则返回None
    """
    text_lower = text.lower()
    
    # 遍历所有关键词映射
    for emotion_type, emotion_data in KEYWORD_EMOTION_MAP.items():
        for keyword in emotion_data["keywords"]:
            if keyword in text_lower:
                return {
                    "file": emotion_data["file"],
                    "emotion": emotion_type,
                    "description": emotion_data["description"],
                    "match_keyword": keyword
                }
    
    return None


def get_emotion_from_anxiety_score(score: float) -> Dict:
    """
    基于焦虑分值获取情绪（基础映射）
    
    Args:
        score: 焦虑分值 [0-10]
        
    Returns:
        dict: 情绪信息
    """
    for (min_val, max_val), emotion_info in ANXIETY_BASED_EMOTIONS.items():
        if min_val <= score < max_val:
            return emotion_info
    
    # 默认返回思考表情
    return {"file": "thinking.png", "emotion": "thinking", "description": "中立思考"}


def get_emotion_from_ai_label(ai_emotion: str) -> Dict:
    """
    将AI返回的情绪标签转为表情文件
    
    Args:
        ai_emotion: AI识别的情绪标签
        
    Returns:
        dict: 情绪信息
    """
    if ai_emotion in AI_EMOTION_LABELS:
        file = AI_EMOTION_LABELS[ai_emotion]
        return {
            "file": file,
            "emotion": ai_emotion,
            "description": f"AI识别: {ai_emotion}"
        }
    
    # 默认返回思考表情
    return {"file": "thinking.png", "emotion": "thinking", "description": "默认表情"}


def get_nomi_expression(
    anxiety_score: float,
    combined_text: str,
    ai_emotion: Optional[str] = None
) -> Dict:
    """
    综合判断Nomi表情（多策略融合）
    
    **🔧 修改**: 已禁用关键词匹配，仅使用 AI 分析
    
    优先级：
    1. ~~关键词匹配（已禁用）~~
    2. AI情绪标签
    3. 焦虑分值（基础映射）
    
    Args:
        anxiety_score: 焦虑分值
        combined_text: 用户输入的完整文本
        ai_emotion: AI识别的情绪标签（可选）
        
    Returns:
        dict: 最终情绪信息
    """
    # 🔧 策略1: 关键词匹配 - 已禁用以验证纯 AI 工作
    # keyword_emotion = detect_emotion_from_keywords(combined_text)
    # if keyword_emotion:
    #     return keyword_emotion
    
    # 策略2: AI情绪标签
    if ai_emotion:
        ai_based = get_emotion_from_ai_label(ai_emotion)
        if ai_based["emotion"] != "thinking":  # 如果不是默认值
            return ai_based
    
    # 策略3: 焦虑分值（基础映射）
    return get_emotion_from_anxiety_score(anxiety_score)


# ============================================================================
# Qwen-2.5 提示词（扩展版）
# ============================================================================

ENHANCED_SYSTEM_PROMPT = """# 角色定位
你是 MindNest 的疗愈伴侣 Nomi,一位温柔、睿智、善于倾听的挚友。

# 语气风格
- 亲切且口语化 (例如使用"哇"、"慢慢来才比较快"等)
- 拒绝说教,不使用学术术语
- 温暖、共情、鼓励为主

# 回复结构 (必须严格遵守)
第一部分:用一句话精准识别用户日记中的核心痛点并给予情感反馈
第二部分:进行深度的心理共情分析,肯定用户的努力
第三部分:给出 1-2 条具体的轻量级疗愈建议 (如:深呼吸、散步、听音乐)

# 数据输出格式 (JSON)
除了文字回复,你必须同时返回以下JSON格式数据:
{
  "anxiety_score": <0-10的浮点数>,
  "reason": "<你的文字回复内容>",
  "emotion": "<情绪标签: happy/sad/worried/confused/thinking/celebrating等>",
  "healing_path": "<疗愈路径: light/moderate/severe>",
  "emotion_details": {
    "迷茫": <0-100百分比>,
    "压力": <0-100百分比>,
    "其他情绪": <0-100百分比>
  }
}

# 评分标准与疗愈路径判定
- 0-3.5: 轻度焦虑或积极情绪 → healing_path: "light" (仅需呼吸练习)
- 3.5-7: 中度焦虑,有明显情绪波动 → healing_path: "moderate" (需要呼吸+利他疗愈)
- 7-10: 重度焦虑,需要立即介入 → healing_path: "severe" (需要完整疗愈套餐:呼吸+利他+行为激活)

# 情绪标签选项
greeting(打招呼), approval(认可), rejection(拒绝), happy(开心), sad(伤心), angry(生气), surprised(惊讶), celebrating(庆祝), encouraging(鼓励), grateful(感恩), thinking(思考), confused(困惑), pleading(请求帮助), relaxing(放松), meditating(冥想), sleepy(困倦), eating(干饭), rushing(赶DDL), lucky(好运), wealthy(暴富), loving(表达爱意), approving(点赞), playful(调皮), extremely_stressed(极度焦虑)

# 示例
用户输入:"我怀疑自己是不是不适合做科研,论文看得这么慢,又看不懂,写related work也写不好"

你的回复JSON:
{
  "anxiety_score": 5.8,
  "reason": "哇,你这个问题真是直击要害啊!其实很多人都有过类似的怀疑。你看,我之前了解到的你,是一个对'无意义消耗'非常敏感的人,而科研恰恰需要大量的时间和精力投入,这可能让你感到特别挫败。但换个角度看,这种'慢'和'看不懂'其实是你在认真对待每一个细节的表现。至于写related work写不好,那可能是因为你还在摸索适合自己的方法,而不是你能力不足。我建议你可以深呼吸几次,然后给自己一个小小的休息,去散散步或者听听音乐,放松一下~慢慢来才比较快,你觉得呢?",
  "emotion": "confused",
  "healing_path": "moderate",
  "emotion_details": {
    "迷茫": 40,
    "压力": 35,
    "自我怀疑": 25
  }
}

请根据用户的日记和对话内容,以Nomi的身份生成符合上述格式的JSON响应。确保healing_path字段严格按照anxiety_score判定。"""


def get_all_expressions() -> List[str]:
    """获取所有24个表情文件名列表"""
    all_files = set()
    
    # 从焦虑映射中提取
    for emotion_info in ANXIETY_BASED_EMOTIONS.values():
        all_files.add(emotion_info["file"])
    
    # 从关键词映射中提取
    for emotion_data in KEYWORD_EMOTION_MAP.values():
        all_files.add(emotion_data["file"])
    
    # 从AI标签映射中提取
    for file in AI_EMOTION_LABELS.values():
        all_files.add(file)
    
    return sorted(list(all_files))
