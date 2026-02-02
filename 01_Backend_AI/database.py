"""
MindNest Database Module
========================

SQLite数据库模块，支持评估历史记录和用户数据持久化

功能：
1. 用户管理
2. 评估历史记录存储
3. 疗愈完成记录追踪
4. 历史数据查询和趋势分析

作者: MindNest Team
日期: 2026-01-26
"""

from sqlalchemy import create_engine, Column, Integer, String, Float, Text, DateTime, ForeignKey
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, relationship
from datetime import datetime
import json
from typing import List, Dict, Optional

# ============================================================================
# 数据库配置
# ============================================================================

DATABASE_URL = "sqlite:///./mindnest.db"

engine = create_engine(
    DATABASE_URL,
    connect_args={"check_same_thread": False},  # 允许多线程访问
    echo=False  # 生产环境设为False
)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

Base = declarative_base()


# ============================================================================
# 数据模型
# ============================================================================

class User(Base):
    """用户表"""
    __tablename__ = "users"
    
    user_id = Column(String, primary_key=True, index=True)
    username = Column(String, nullable=True)
    created_at = Column(DateTime, default=datetime.now)
    last_active = Column(DateTime, default=datetime.now, onupdate=datetime.now)
    
    # 关系
    assessments = relationship("AssessmentHistory", back_populates="user")


class AssessmentHistory(Base):
    """评估历史记录表"""
    __tablename__ = "assessment_history"
    
    id = Column(Integer, primary_key=True, autoincrement=True, index=True)
    user_id = Column(String, ForeignKey("users.user_id"), nullable=False, index=True)
    
    # 评估结果
    anxiety_score = Column(Float, nullable=False)
    anxiety_level = Column(String, nullable=False)  # light/moderate/severe
    
    # 疗愈方案（存储为JSON字符串）
    healing_suite = Column(Text, nullable=False)  # ["breathing", "altruistic"]
    nutrients = Column(Text, nullable=False)  # {"sunlight": 10, "water": 15}
    total_nutrients = Column(Integer, nullable=False)
    
    # Nomi反馈
    nomi_expression = Column(String, nullable=True)
    nomi_emotion = Column(String, nullable=True)
    nomi_state = Column(String, nullable=True)
    
    # 任务（如有）
    task = Column(Text, nullable=True)
    
    # 原始输入
    diary_text = Column(Text, nullable=True)
    conversation_text = Column(Text, nullable=True)
    
    # AI分析
    ai_reasoning = Column(Text, nullable=True)
    
    # 时间戳
    created_at = Column(DateTime, default=datetime.now, index=True)
    
    # 关系
    user = relationship("User", back_populates="assessments")


class HealingCompletion(Base):
    """疗愈完成记录表（用于跟踪疗愈效果）"""
    __tablename__ = "healing_completion"
    
    id = Column(Integer, primary_key=True, autoincrement=True)
    user_id = Column(String, ForeignKey("users.user_id"), nullable=False)
    assessment_id = Column(Integer, ForeignKey("assessment_history.id"), nullable=False)
    
    # 完成详情
    healing_mode = Column(String, nullable=False)  # breathing/altruistic/behavioral_activation
    duration_seconds = Column(Integer, nullable=True)
    post_anxiety_score = Column(Float, nullable=True)  # 疗愈后的焦虑分值
    
    # 时间戳
    completed_at = Column(DateTime, default=datetime.now)


# ============================================================================
# 数据库初始化
# ============================================================================

def init_db():
    """初始化数据库，创建所有表"""
    Base.metadata.create_all(bind=engine)
    print("✅ 数据库初始化完成")


def get_db():
    """获取数据库会话（依赖注入用）"""
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


# ============================================================================
# CRUD操作
# ============================================================================

def create_user(db, user_id: str, username: str = None) -> User:
    """创建用户（如不存在）"""
    user = db.query(User).filter(User.user_id == user_id).first()
    if not user:
        user = User(user_id=user_id, username=username)
        db.add(user)
        db.commit()
        db.refresh(user)
    return user


def save_assessment(
    db,
    user_id: str,
    anxiety_score: float,
    anxiety_level: str,
    healing_suite: List[str],
    nutrients: Dict[str, int],
    total_nutrients: int,
    nomi_expression: str,
    nomi_emotion: str,
    nomi_state: Optional[str],
    task: Optional[str],
    diary_text: str,
    conversation_text: str,
    ai_reasoning: str
) -> AssessmentHistory:
    """保存评估记录"""
    # 确保用户存在
    create_user(db, user_id)
    
    # 创建评估记录
    assessment = AssessmentHistory(
        user_id=user_id,
        anxiety_score=anxiety_score,
        anxiety_level=anxiety_level,
        healing_suite=json.dumps(healing_suite, ensure_ascii=False),
        nutrients=json.dumps(nutrients, ensure_ascii=False),
        total_nutrients=total_nutrients,
        nomi_expression=nomi_expression,
        nomi_emotion=nomi_emotion,
        nomi_state=nomi_state,
        task=task,
        diary_text=diary_text,
        conversation_text=conversation_text,
        ai_reasoning=ai_reasoning
    )
    
    db.add(assessment)
    db.commit()
    db.refresh(assessment)
    
    return assessment


def get_user_history(db, user_id: str, limit: int = 7) -> List[AssessmentHistory]:
    """获取用户最近的评估历史"""
    return db.query(AssessmentHistory)\
        .filter(AssessmentHistory.user_id == user_id)\
        .order_by(AssessmentHistory.created_at.desc())\
        .limit(limit)\
        .all()


def get_assessment_stats(db, user_id: str) -> Dict:
    """获取用户评估统计数据"""
    assessments = db.query(AssessmentHistory)\
        .filter(AssessmentHistory.user_id == user_id)\
        .order_by(AssessmentHistory.created_at.desc())\
        .all()
    
    if not assessments:
        return {
            "total_assessments": 0,
            "average_score": 0.0,
            "trend": "no_data",
            "lowest_score": 0.0,
            "highest_score": 0.0
        }
    
    scores = [a.anxiety_score for a in assessments]
    
    # 计算趋势（对比最近3次和之前3次的平均值）
    trend = "stable"
    if len(scores) >= 6:
        recent_avg = sum(scores[:3]) / 3
        older_avg = sum(scores[3:6]) / 3
        if recent_avg < older_avg - 1.0:
            trend = "improving"
        elif recent_avg > older_avg + 1.0:
            trend = "worsening"
    
    return {
        "total_assessments": len(assessments),
        "average_score": round(sum(scores) / len(scores), 2),
        "trend": trend,
        "lowest_score": round(min(scores), 2),
        "highest_score": round(max(scores), 2)
    }


def save_healing_completion(
    db,
    user_id: str,
    assessment_id: int,
    healing_mode: str,
    duration_seconds: int,
    post_anxiety_score: Optional[float] = None
) -> HealingCompletion:
    """保存疗愈完成记录"""
    completion = HealingCompletion(
        user_id=user_id,
        assessment_id=assessment_id,
        healing_mode=healing_mode,
        duration_seconds=duration_seconds,
        post_anxiety_score=post_anxiety_score
    )
    
    db.add(completion)
    db.commit()
    db.refresh(completion)
    
    return completion


# ============================================================================
# 辅助函数
# ============================================================================

def assessment_to_dict(assessment: AssessmentHistory) -> Dict:
    """将评估记录转为字典（用于API响应）"""
    return {
        "id": assessment.id,
        "user_id": assessment.user_id,
        "anxiety_score": assessment.anxiety_score,
        "anxiety_level": assessment.anxiety_level,
        "healing_suite": json.loads(assessment.healing_suite),
        "nutrients": json.loads(assessment.nutrients),
        "total_nutrients": assessment.total_nutrients,
        "nomi_expression": assessment.nomi_expression,
        "nomi_emotion": assessment.nomi_emotion,
        "task": assessment.task,
        "created_at": assessment.created_at.isoformat()
    }
