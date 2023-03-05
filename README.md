# BKTrans

BKTrans的功能设计上很简单，截图，识别出文字，最后翻译出文本，力求简洁和高效。

## 使用

BKTrans目前使用在线调用百度OCR和Windows10本地OCR作为截图后的文本识别，百度翻译和彩云小译作为文本翻译的模式来运作。

### 下载

下载地址：https://github.com/blackier/BKTrans/releases

有两种压缩包：

- 有`net6`后缀的，是已经把`.net 6`运行时打包进去，压缩包会比较大，但不需要自己安装运行时，解压后就可以运行。
- 没有`net6`后缀的，需要自己安装最新的`.net桌面运行时`，下载地址：https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0

开发和测试都是在`win10 x64`上，最低版本支持是1903，可能会因操作系统不同而无法运行。

### 配置

这些翻译API都需要用户自己开通账号和申请API参数，要注意，这些API都是有限制的，超出后要付费，不是频繁使用，每月免费次数基本够用了，而也并非一成不变，留意官方的通知。

百度OCR必配置`client_id`和`client_secret`：https://ai.baidu.com/ai-doc/REFERENCE/Ck3dwjhhu

百度翻译必配置`appid`和`secretkey`：https://fanyi-api.baidu.com/product/113

彩云小译必配置`token`：https://docs.caiyunapp.com/blog/2018/09/03/lingocloud-api

官方文档里都说明了如何获取这些参数，请认真看下文档，看不明白网上搜索下也有通俗易懂的教程。

在设置的API选项卡里填写对应的参数后，点击确定保存后就可以使用了，用不明白的也可以看这个视频详细了解下：[BV]()。

**注意事项：**

- 本地OCR需要对应的语言支持，需要在系统设置中添加需要的语言，只测试过日语和简体中文，其他语言未测试。

- 百度OCR使用的是通用文字识别（标准版），目前是每月免费1000次，超出后收费[不便宜](https://cloud.baidu.com/doc/OCR/s/9k3h7xuv6)，酌情使用和充值。

- 百度翻译开通后，默认是标准版(1QPS)，认证后可以升级到高级版，高级版每秒可访问次数是10(10QPS)，如果使用了自动翻译且没认证，那么会容易触发限制导致翻译失败。认证后需要手动在开发者信息控制台切换到高级版。

### OCR替换

OCR识别出的文字可能有误，或者想把识别出的某些文本直接替换成固定词汇，这样在翻译时更有准确度，那么可以在设置配置OCR替换。

默认的是一个空白配置，可以添加和删除配置。这个空白配置可以保留，当不想替换时就切换成这个空白配置就好。

添加配置：

- 点击添加激活输入框，输入新配置的名称，再点击添加就会切换到新的空白配置里进行设置。
- 在下面的表格空白行双击就可以填入要替换的文本。
- 要删除某行，选中那行后，按`Delete`键删除。

删除配置：

- 下拉框选择配置，点击删除。
- 全部删除后，会自动生成一空白的配置。

关闭设置界面回到主窗体，就可以在配置的下拉框中选择需要的配置进行替换了。当选择不为空的配置进行替换时，OCR识别出的文本会用`+++===+++===+++`进行分割，上面的是替换后的文本，下面的识别出的原文本。这时候如果手动编辑来进行再次翻译，需要编辑上面的文本。

**注意事项**：

- 添加和删除配置都是实时生效的，底下的取消和确定对OCR替换不生效。
- 文本替换是串行的全文替换，比如：`123456`，替换文本是`123->222`和`24->34`，那么会先替换成`222456`，接着被替换成`223456`。多文本替换时注意顺序和关系。目前不支持应用内调整顺序，只能关闭应用后，手动在`setting.json`里调整。

### 自动翻译

BKTrans的系统托盘选项里，有是否开启`自动翻译`的选项，这个功能需要显示有翻译浮窗，才会生效。

当你使用快捷键`F2`或者系统托盘的翻译来进行截图翻译，显示有翻译浮窗时，如果激活了这个功能，那么BKTrans会定时去监控上次截取的窗体区域是否有发生改变，当发生改变时，会自动截图翻译。

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

BKTrans的所有设置都会保存在exe所在目录的`setting.json`文件里，里面列出了所有的配置，包括API和OCR替换的。

某些设置肯能是不会有UI界面进行配置，如果需要更多的自定义，则要把BKTrans关闭后，手动编辑`setting.json`，更改保存后重启生效。下面列出了部分配置：

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

BKTrans的实现逻辑并不复杂，对于有桌面客户端开发经验的人甚至可以说很简单，整个项目依托于vs2022和.net6，安装好环境后，clone到本地就可以直接编译开发了。

目前，考虑到复杂度，只支持百度OCR和本地OCR，而添加翻译API时，注意支持的翻译语种与OCR的翻译语种是能匹配的，最起码匹配一种。

### 框架

- Visual Studio 2022
- .NET 7

### 规范

一般，一个类从上到下：字段，属性，类方法，自定义方法，事件方法。大体如此，按代码类型有序存放。

c#的代码规范，使用的是[godot](https://github.com/godotengine/godot)的[C# 风格指南](https://docs.godotengine.org/zh_CN/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html)，这是一份比较全面且合理的c#代码规范，但也有不一样的：

- 使用换行符(`CRLF`)来换行。
- 使用带字节顺序标记(`BOM`) 的`UTF-8`编码。
- 代码提交时需要格式化，提交记录请使用`type(scope): message`的格式提交。
- 注释和提交记录请使用中文，但变量命名请使用英文单词，而不是拼音。

关于类的字段的命名，godot的[命名约定](https://docs.godotengine.org/zh_CN/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html#naming-conventions)中，只有私有字段才会使用下划线(`_`)加小字母开头的驼峰命名法来命名，但BKTrans中则是在类中，无论是什么类型，只有是成员字段，都采用这种下划线开头的命名法，属性则还是大写字母开头命名。

比如godot中会是这样：

```c#
namespace ExampleProject
{
    public class PlayerCharacter
    {
        public const float DefaultSpeed = 10f;

        public float CurrentSpeed { get; set; }

        protected int HitPoints;

        private Vector3 _aimingAt; // Use a `_` prefix for private fields.

        private void Attack(float attackStrength)
        {
            Enemy targetFound = FindTarget(_aimingAt);

            targetFound?.Hit(attackStrength);
        }

    }
}
```

BKTrans中则是：

```c#
namespace ExampleProject
{
    public class PlayerCharacter
    {
        public const float _defaultSpeed = 10f;

        public float CurrentSpeed { get; set; }

        protected int _hitPoints;

        private Vector3 _aimingAt; // Use a `_` prefix for private fields.

        private void Attack(float attackStrength)
        {
            Enemy targetFound = FindTarget(_aimingAt);

            targetFound?.Hit(attackStrength);
        }

    }
}
```

此外，关于xaml中控件的name的命名，则是采用控件类型加下划线的全小写字母的的命名法，比如翻译按钮控件的名称为`button_trans`，翻译结果的为`textbox_trans`，控件类型开头，拼接控件具体作用。

可以提pr，但请遵循以上比较繁琐的开发规范，当然，项目本身很简单，只要在`GPL v3`的许可下，自由修改使用。

## 第三方库

- [ShareX](https://github.com/ShareX/ShareX)
## 开源许可

- GPL v3
