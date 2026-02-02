# MindNest Mobile App

React + Tailwind CSS 移动端原型，完美还原UI设计

## 🎨 设计还原

- ✅ 奶油白配色方案 (#F5F5F0)
- ✅ 灰绿色主题 (#A8B5A5)
- ✅ 大圆角卡片设计
- ✅ 底部三标签导航
- ✅ 呼吸感动画效果

## 📦 安装依赖

```bash
cd 02_Mobile_App
npm install
```

## 🚀 启动开发服务器

```bash
npm start
```

应用将在 http://localhost:3000 打开

## 📱 功能页面

### Record 页面
- 心情表情选择器（5个emoji）
- 主题和内容输入
- 调用 POST /api/v1/assess
- Nomi反馈弹窗
- 历史记录列表

### Report 页面
- 焦虑水平趋势图（Chart.js）
- 调用 GET /api/v1/history/{user_id}
- 统计数据展示
- 成就卡片

### Plant 页面
- 养料统计展示（占位）

## 🔗 API对接

确保后端服务运行在 `http://localhost:8000`

修改 `API_BASE_URL` 如需更改地址。

## 🎯 UI还原度

| 元素 | 还原度 |
|------|--------|
| 配色方案 | 100% |
| 圆角卡片 | 95% |
| 字体排版 | 90% |
| 动画效果 | 85% |

## 📝 开发笔记

- React 18 + Hooks
- Tailwind CSS 3.4 自定义主题
- Chart.js 折线图
- Axios API调用
- 响应式设计

## 🚧 待优化

- [ ] 实际Nomi表情图片嵌入
- [ ] Plant页面Three.js集成
- [ ] PWA支持
- [ ] 性能优化

---

**开发团队:** MindNest  
**最后更新:** 2026-01-26
