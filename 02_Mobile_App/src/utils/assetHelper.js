/**
 * MindNest - Asset Helper Utilities
 * ==================================
 * 
 * Nomi 表情图片 URL 生成和 fallback 逻辑
 * 
 * 作者: MindNest Team
 * 日期: 2026-01-26
 */

/**
 * 生成 Nomi 表情图片 URL
 * @param {string} expressionFileName - 表情文件名（如："happy.png"）
 * @returns {string} 图片 URL
 */
export const getNomiEmojiUrl = (expressionFileName) => {
    if (!expressionFileName) {
        return '/assets/nomi/thinking.png'; // 默认表情
    }
    return `/assets/nomi/${expressionFileName}`;
};

/**
 * 表情加载失败时的 emoji fallback
 * @param {string} expressionFileName - 表情文件名
 * @returns {string} emoji 字符
 */
export const getNomiEmojiFallback = (expressionFileName) => {
    const fallbackMap = {
        'cpu_burned.png': '🤯',
        'welcome.png': '👋',
        'no.png': '🙅',
        'ok.png': '👌',
        'sad.png': '😢',
        'cheer.png': '💪',
        'eating.png': '🍚',
        'celebrate.png': '🎉',
        'happy.png': '😊',
        'thinking.png': '🤔',
        'surprise.png': '😲',
        'please.png': '🙏',
        'slacking.png': '🐟',
        'meditation.png': '🧘',
        'goodnight.png': '😴',
        'rich.png': '💰',
        'love.png': '❤️',
        'like.png': '👍',
        'angry.png': '😠',
        'question.png': '❓',
        'naughty.png': '😜',
        'thanks.png': '🙏',
        'deadline.png': '⏰',
        'lucky.png': '🐠'
    };

    return fallbackMap[expressionFileName] || '😊';
};

/**
 * 预加载所有 Nomi 表情图片
 * 提升首次显示速度
 */
export const preloadNomiExpressions = () => {
    const expressions = [
        'cpu_burned.png', 'welcome.png', 'no.png', 'ok.png',
        'sad.png', 'cheer.png', 'eating.png', 'celebrate.png',
        'happy.png', 'thinking.png', 'surprise.png', 'please.png',
        'slacking.png', 'meditation.png', 'goodnight.png', 'rich.png',
        'love.png', 'like.png', 'angry.png', 'question.png',
        'naughty.png', 'thanks.png', 'deadline.png', 'lucky.png'
    ];

    expressions.forEach(fileName => {
        const img = new Image();
        img.src = getNomiEmojiUrl(fileName);
    });

    console.log('✅ Nomi 表情预加载完成');
};
