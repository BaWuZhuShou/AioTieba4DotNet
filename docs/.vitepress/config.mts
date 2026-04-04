import { defineConfig } from 'vitepress';

const repoUrl = 'https://github.com/BaWuZhuShou/AioTieba4DotNet';

// Keep README-relative links from being reported as dead links during VitePress builds.
const readmeDeadLinkAllowlist = [
  './../README',
  './../README.md',
  '../README',
  '../README.md',
  './../../README',
  './../../README.md',
  '../../README',
  '../../README.md'
];

export default defineConfig({
  lang: 'zh-CN',
  title: 'AioTieba4DotNet',
  description: '面向 .NET 10 的异步贴吧客户端文档。',
  ignoreDeadLinks: readmeDeadLinkAllowlist,
  markdown: {
    lineNumbers: true
  },
  themeConfig: {
    nav: [
      { text: '首页', link: '/' },
      { text: '快速开始', link: '/guide/getting-started' },
      {
        text: '使用场景',
        items: [
          { text: '贴吧相关', link: '/how-to/forums' },
          { text: '帖子相关', link: '/how-to/threads' },
          { text: '用户相关', link: '/how-to/users' },
          { text: '消息相关', link: '/how-to/messages' },
          { text: '吧务相关', link: '/how-to/admins' }
        ]
      },
      { text: 'API 参考', link: '/reference/modules' },
      { text: '进阶', link: '/guide/advanced' },
      { text: '排障', link: '/guide/troubleshooting' },
      { text: 'GitHub', link: repoUrl }
    ],
    sidebar: [
      {
        text: '开始使用',
        items: [{ text: '快速开始', link: '/guide/getting-started' }]
      },
      {
        text: '使用场景',
        items: [
          { text: '贴吧相关', link: '/how-to/forums' },
          { text: '帖子相关', link: '/how-to/threads' },
          { text: '用户相关', link: '/how-to/users' },
          { text: '消息相关', link: '/how-to/messages' },
          { text: '吧务相关', link: '/how-to/admins' }
        ]
      },
      {
        text: '参考与深入',
        items: [
          { text: 'API 参考', link: '/reference/modules' },
          { text: '进阶', link: '/guide/advanced' },
          { text: '排障', link: '/guide/troubleshooting' }
        ]
      },
      {
        text: '相关说明',
        collapsed: true,
        items: [
          { text: '迁移 v2 → v3', link: '/related/migration-v2-to-v3' },
          { text: '发布说明 v3', link: '/related/release-notes-v3' },
          { text: '对齐台账', link: '/related/parity' }
        ]
      },
      {
        text: '存档',
        collapsed: true,
        items: [{ text: '历史 Todo', link: '/archive/todo' }]
      }
    ],
    search: {
      provider: 'local'
    },
    outline: {
      level: [2, 3],
      label: '本页目录'
    },
    editLink: {
      pattern: `${repoUrl}/edit/master/docs/:path`,
      text: '在 GitHub 上编辑此页'
    },
    docFooter: {
      prev: '上一页',
      next: '下一页'
    }
  }
});
