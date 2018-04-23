[![Build status](https://ci.appveyor.com/api/projects/status/66dy10xgquyw3y7x?svg=true)](https://ci.appveyor.com/project/2mas/selectnextoccurrence)
# Select Next Occurrence

Download this extension from the [VS Gallery](https://marketplace.visualstudio.com/items?itemName=thomaswelen.SelectNextOccurrence) or get the latest [CI-build](http://vsixgallery.com/extension/NextOccurrence.b213c4e9-b96f-4f9d-b1d6-fa8bc7e9da21/).

---------------------------------------

This extension makes it possible to select next occurrences of a selected text for editing.

Aims to replicate the Ctrl+D command of Sublime Text for faster coding.

See the [change log/roadmap](CHANGELOG.md) for changes and Roadmap.

## Features

- Select next occurrence of current selection
- Select previous occurrence
- Select all occurrences
- Convert a selection into multiple cursors
- Skip occurrence
- Undo occurrence
- Add caret above/below
- Use multiple carets to edit
  - Alt-Click to add caret

![Select Next Occurrence](select_next.gif)

---------------------------------------

### Options
- Toggle case-sensitive search by using the "Match case" setting from the find-dialog (Ctrl+F)
- Deactivate Alt-Clicking to add carets in options-dialog

![Select Next Occurrence Options dialog](settings.png)

---------------------------------------

### Key-bindings
Go to Tools -> Options -> Keyboard and search for these command names to edit at your choice. Make sure the _Use new shortcut in_ is set to: ***Text Editor***

| Command (prefix ```SelectNextOccurrence.```) | Recommendation |
| :--- | :--- |
| ```SelectNextOccurrence``` | Ctrl+D |
| ```SelectPreviousOccurrence``` | Ctrl+E |
| ```SelectAllOccurrences``` | Ctrl+K, Ctrl+A |
| ```SkipOccurrence``` | Ctrl+K, Ctrl+D |
| ```UndoOccurrence``` | Ctrl+U |
| ```AddCaretAbove``` | Ctrl+Alt+Up |
| ```AddCaretBelow``` | Ctrl+Alt+Down |
| ```ConvertSelectionToMultipleCursors``` | Ctrl+Shift+I or Alt+Shift+I (vscode)) |

![Select Next Occurrence Keyboard bindings](kbd_shortcuts.png)

---------------------------------------

### Troubleshooting

- **Nothing happens when pressing assigned keys**

Check that the key-bindings are correct and that the _Use new shortcut in_ is set to: ***Text Editor***.

- **Nothing happens when ALT + left-clicking mouse button to add new caret, multiple edits are unresponsive**

There is a possibility that other plugins use this functionality too, and a conflict occurs. Please check for other installed plugins with this feature and try to disable.

- **Copy/cut multiple occurrences doesnt work as expected**

There is a conflict with the extension Copy As Html, if you have this enabled, try to disable it and see if this helps.

---------------------------------------

### Contribute
Check out the [contribution guidelines](CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the
[Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project. 

As far as I know you must have a 2015-edition of VS installed (or previously installed) to get the generators working for Extensibility Tools in 2017.

### License
[Apache 2.0](LICENSE)