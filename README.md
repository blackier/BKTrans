# BKTrans

BKTrans的功能设计上很简单，截图，识别出文字，最后翻译出文本，力求简洁和高效。

## 使用

BKTrans目前使用在线调用百度OCR作为截图后的文本识别，百度翻译和彩云小译作为文本翻译的模式来运作。

### 下载

下载地址：https://github.com/blackier/BKTrans/releases

有两种压缩包：

- 有`net6`后缀的，是已经把`.net 6`运行时打包进去，压缩包会比较大，但不需要自己安装运行时，解压后就可以运行。
- 没有`net6`后缀的，需要自己安装最新的`.net桌面运行时`，下载地址：https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0

开发和测试都是在`win10 x64`上，可能会因操作系统不同而无法运行。

### 配置

这些翻译API都需要用户自己开通账号和申请API参数，要注意，这些API都是有调用次数限制的，超过后要付费，但也不贵，不是频繁使用，每月免费次数基本够用了，而这些API也并非一成不变，留意官方的通知。

百度OCR需要配置`client_id`和`client_secret`：https://ai.baidu.com/ai-doc/REFERENCE/Ck3dwjhhu

百度翻译需要配置`appid`和`secretkey`：https://fanyi-api.baidu.com/product/113

彩云小译需要配置`token`：https://docs.caiyunapp.com/blog/2018/09/03/lingocloud-api

官方文档里都说明了如何获取这些参数，请认真看下文档，看不明白网上搜索下也有通俗易懂的教程。

在设置里填写对应的参数后，就可以使用了，使用并不难，不明白的也可以看这个视频详细了解下：[BV]()。

### 快捷键

有提供快捷键，但为了不按键冲突，设置的比较奇葩，目前也不支持自定义更改：

`F2`：截图翻译。

`Shift+Alt+X`：截取上一次屏幕区域进行翻译，并显示翻译浮窗。

`Shift+Alt+Z`：隐藏翻译窗体。

## 开发

BKTrans的实现逻辑并不复杂，对于有桌面客户端开发经验的人甚至可以说很简单，整个项目依托于vs2022和.net6，安装好环境后，clone到本地就可以直接编译开发了。

目前，考虑到复杂度，只打算一OCR对多翻译API的形式，百度的OCR足够用了，本身更考验翻译API的翻译质量，而添加翻译API时，注意支持的翻译语种与OCR的翻译语种是能匹配的。

### 框架

- Visual Studio 2022
- .NET 6

### 规范

一般，一个类从上到下：字段，属性，类方法，自定义方法，事件方法。大体如此，按代码类型有序存放。

c#的代码规范，使用的是[godot](https://github.com/godotengine/godot)的[C# 风格指南](https://docs.godotengine.org/zh_CN/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html)，这是一份比较全面且合理的c#代码规范，但也有不一样的：

- 使用换行符(`CRLF`)来换行。
- 使用带字节顺序标记(`BOM`) 的`UTF-8`编码。
- 代码提交时需要格式化，提交记录请使用`type(scope): message`的格式提交。
- 注释和提交记录请使用中文，详实明了。

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
