# BKTrans

[![Licence](https://img.shields.io/badge/license-GPLv3-blue.svg?style=flat-square&label=License)](./LICENSE)
[![Latest Release](https://img.shields.io/github/release-pre/blackier/BKTrans.svg?style=flat-square&label=Latest%20Release)](https://github.com/blackier/BKTrans/releases)
[![Download Stats](https://img.shields.io/github/downloads/blackier/BKTrans/total.svg?style=flat-square&label=Downloads)](https://github.com/blackier/BKTrans/releases)

BKTrans的功能设计上很简单，截图，识别出文字，最后翻译出文本，力求简洁和高效。

## 使用

### 下载

下载地址：https://github.com/blackier/BKTrans/releases

有两种压缩包：

- 有`net`后缀的，是已经把`.net`运行时打包进去，压缩包会比较大，但不需要自己安装运行时，解压后就可以运行。
- 没有`net`后缀的，需要自己安装对应的`.net桌面运行时`，下载地址：https://dotnet.microsoft.com/zh-cn/download/dotnet

| BKTrans | .net   |
| ------- | ------ |
| v0.3.0  | .net 8 |
| v0.2.0  | .net 7 |
| v0.1.0  | .net 6 |

开发和测试都是在`win10 x64`上，最低版本支持是1903，可能会因操作系统不同而无法运行。

### API配置

目前支持翻译组合：

| OCR           | 翻译       |
| ------------- | ---------- |
| 百度OCR       | 百度翻译   |
| Microsoft OCR | 彩云小译   |
|               | Google翻译 |

- 百度OCR必配置`client_id`和`client_secret`：https://ai.baidu.com/ai-doc/REFERENCE/Ck3dwjhhu

- Microsoft OCR，实际是Windows10自带的OCR功能，需要在系统的语言设置中添加需要的语言，只测试过日语和简体中文，其他语言未测试。添加完语言后重启电脑才会生效。

- 百度翻译必配置`appid`和`secretkey`：https://fanyi-api.baidu.com/product/113

- 彩云小译必配置`token`：https://docs.caiyunapp.com/blog/2018/09/03/lingocloud-api

- Google翻译必配置`api_key`，但是申请很麻烦，需要国外的信用卡才行，而且因为本身的翻译流程导致和OCR替换配合翻译结果其实不怎么理想，但是在些活动说明和官方文档之类的翻译上，效果就不错。如果有需求就自己探索怎么申请开通吧。

这些翻译API都需要用户自己开通账号和申请API参数，要注意，这些API都是有限制的，超出后要付费，不是频繁使用，每月免费次数基本够用了，而也并非一成不变，留意官方的通知。

基本上官方文档里都说明了如何获取这些参数，请认真看下文档，看不明白网上搜索下也有通俗易懂的教程。

在设置的API选项卡里填写对应的参数后，点击确定保存后就可以使用了，用不明白的也可以看这个视频详细了解下：[BV]()。

**注意事项：**

- 百度OCR使用的是通用文字识别（标准版），目前是每月免费1000次，超出后收费[不便宜](https://cloud.baidu.com/doc/OCR/s/9k3h7xuv6)，酌情使用和充值。

- 百度翻译开通后，默认是标准版(1QPS)，认证后可以升级到高级版，高级版每秒可访问次数是10(10QPS)，如果使用了自动翻译且没认证，那么会容易触发限制导致翻译失败。认证后需要手动在开发者信息控制台切换到高级版。

### OCR替换

OCR识别出的文字可能有误，或者想把识别出的某些文本直接替换成固定词汇，这样在翻译时更有准确度，那么可以在设置配置OCR替换。

默认的是一个空白配置，可以添加和删除配置。这个空白配置可以保留，当不想替换时就切换成这个空白配置就好。

添加配置：

- 输入框输入新配置的名称，再点击添加就会切换到新的空白配置里进行设置。
- 在下面的表格空白行双击就可以填入要替换的文本。
- 要删除某行，选中那行后，按`Delete`键删除。

删除配置：

- 下拉框选择配置，点击删除。
- 全部删除后，会自动生成一空白的配置。

关闭设置界面回到主窗体，就可以在配置的下拉框中选择需要的配置进行替换了。当选择不为空的配置进行替换时，OCR识别出的文本会用`+++===+++===+++`进行分割，上面的是替换后的文本，下面的识别出的原文本。这时候如果手动编辑来进行再次翻译，需要编辑上面的文本。

**注意事项**：

- 添加和删除配置都是实时生效的。
- 文本替换是按序的全文替换，排在前面的被替换了，就不会被后面的替换。多文本替换时注意顺序和关系，可以在编辑页面手动拖拽调整顺序。

### 自动翻译

当你使用快捷键`F2`或者系统托盘的翻译来进行截图翻译，会显示有翻译浮窗，右边的按钮去会有个圆圈箭头的按钮，点击后高亮就会激活自动翻译。

如果激活了这个功能，那么BKTrans会定时去监控上次截取的窗体区域是否有发生改变，当发生改变时，会自动截图翻译。

自动翻译会有一定的误触发，而且使用自动翻译，容易频繁的调用接口，消耗掉免费的接口次数，看情况选择是否开启。

自动翻译设置中，一些选项的含义：

- 采样间隔：自动翻译扫描截图区域是否发生变化的周期，ms。
- 采样倒数：自动翻译发现截图区域发生改变后，再次截图对比的倒数。
- 相似度临界值：自动翻译截图区域相似度，低于这个值则认为发生了改变，重置倒数，当高于这个值则认为改变稳定，开始截图翻译。

目前都是通过鼠标滚轮滚动设置值。

### 快捷键

有提供快捷键，但为了不按键冲突，设置的比较奇葩，目前也不支持自定义更改：

`F2`：截图翻译，并显示翻译浮窗。

`Shift+Alt+X`：截取上一次屏幕区域进行翻译，并显示翻译浮窗。

`Shift+Alt+Z`：隐藏翻译窗体。

### 更多设置

BKTrans的所有设置都会保存在exe所在目录的`BKTransSetting.json`文件里，里面列出了所有的配置，包括API和OCR替换的。

某些设置可能是不会有UI界面进行配置，如果需要更多的自定义，则要把BKTrans关闭后，手动编辑`setting.json`，更改保存后重启生效。下面列出了部分配置：

```json
{
  // ...
  // ocr文本替换，如果你想分享或者新增配置，
  // 可以复制和增加，删除请在UI界面做，避免出错
  "ocr_replace": {
    "": [],
    "XXXX": [

    ]
  }
}
```

## 开发

目前，考虑到复杂度，只支持百度OCR和本地OCR，添加翻译API时，注意支持的翻译语种与OCR的翻译语种是能匹配的，最起码匹配一种。

### 框架

- Visual Studio 2022
- .NET Last Version

## 第三方库

- [ShareX](https://github.com/ShareX/ShareX)
- [wpfui](https://github.com/lepoco/wpfui)

## 开源许可

- GPL v3
