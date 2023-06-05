# MIDIMapper

Map your MIDI device to a macro keyboard/映射你的MIDI键盘为一个宏键盘

其他语言版本
- [English](README/README_en.md)

## 程序功能

一个windows下高自定义的，根据生成或修改后的[配置文件](PGC.json)，将任何MIDI输入设备变为宏键盘的程序。

可手动执行或挂载为服务项

## 注意

**此程序仍在开发迭代中，仍存在BUG与相关功能不支持，遇到相关问题提出issue**

**使用过程如需要自行修改源代码请遵守相关开源许可**

<sub>开发设备依据 : *Alesis V25*</sub>

## 设计目标

- [x] MIDI设备全功能识别

- 支持宏
  - [x] 单次触发
  - [ ] 重复触发
  - [ ] 保持触发
  - [ ] 混合触发

- 支持功能片段
  - [x] 模拟键盘按键/快捷键
  - [ ] 控制延迟
  - [ ] 打开文件/程序