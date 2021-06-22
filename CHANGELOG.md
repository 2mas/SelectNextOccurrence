# Road map
- [ ] Add support for Visual Studio 2022

# Change log


#### 1.3.175
- [x] Bugfix, fix caret jumping to EOF when selecting from intellisense dialog

#### 1.3.172
- [x] Bugfixes on caret positions at document start or EOF
- [x] Enabled selecting next occurrence on multi-line selections
- [x] Adding an option to select occurrences between selections. Enable this in settings. See [[PR]](https://github.com/2mas/SelectNextOccurrence/pull/63) for description of usage

#### 1.3.150
- [x] Adding the ability to remove a caret/selection with ALT-click

#### 1.3.141
- [x] Adding feature from feature request: Setting for choosing first or last entry on escape key - [[ISSUE]](https://github.com/2mas/SelectNextOccurrence/issues/52)
- [x] Fixing vertical navigation/add caret above/below when mixing tabs and spaces - [[ISSUE]](https://github.com/2mas/SelectNextOccurrence/issues/51)

#### 1.3.132
- [x] Overwrite mode supported - [[PR]](https://github.com/2mas/SelectNextOccurrence/pull/48)
- [x] Fixes issue when you copy/cut with only one selection/caret when in multi caret mode so it behaves like normal single selection copy/cut. - [[PR]](https://github.com/2mas/SelectNextOccurrence/pull/47)

Thanks @ Mr-Badger

#### 1.3.114
- [x] Keeping selections with undo/redo, refactoring and bugfixes

#### 1.3.96
- [x] Add Virtual space support
- [x] Rebuilt copy/paste functionality to improve speed and enable pasting of multiple selections into external documents
- [x] Added compatibility for the extension Subword Navigation

#### 1.3.83
- [x] Bugfix: fixes search when wrapping document, caused by the mixing of next/previous introduced in 1.3.80

#### 1.3.80
- [x] Enabling combinations of selecting next/previous occurrences
- [x] Adjustments and bug-fixes involving selections, caret-positions, line-movement etc

Thanks Mr-Badger

#### 1.3.69
- [x] Improved vertical caret movement by keeping column position
- [x] Combines overlapping selections
- [x] Added support for comment/uncomment on multiple selections
- [x] Fixed moving multiple consequitive lines

#### 1.3.52
- [x] Keeping selections when invoking Edit.MakeUppercase and Edit.MakeLowercase - thanks @ Mr-Badger

#### 1.3.49
- [x] Automatically expand regions if selected text is in this region (caused exception before)

#### 1.3.46
- [x] Fixed bug issue [19](https://github.com/2mas/SelectNextOccurrence/issues/19) about Build Configuration dropdown not showing when selecting a file. - thanks @ leachdaniel
- [x] Fixed bug causing multiple copied texts not being copied to static variable when discaring multiple selections.

#### 1.3.44
- [x] Fix of issue [18](https://github.com/2mas/SelectNextOccurrence/issues/18): enabling copy/paste of multiple selections across documents

#### 1.3.40
- [x] Adding commands ```SelectNextExactOccurrence``` and ```SelectPreviousExactOccurrence``` - thanks @ ngzaharias

#### 1.3.32
- [x] Fixing caret-marker height when placing carets on method-declarations etc with CodeLens active

### 1.3.31
- [x] Added support for Visual Studio 2015
- [x] Switched to async package loading

##### 1.2.25
- [x] Toggle match-whole-word search by using the "Match whole word" setting from the find-dialog (Ctrl+F)

##### 1.2.22
- [x] Fixed copying of multiple occurrences and pasting into a single caret

##### 1.2.17
- [x] Added command SelectPreviousOccurrence
##### 1.2.16
- [x] Added options-dialog to disable adding carets by mouse-click
- [x] Corrected interference with block-selection when Alt-dragging mouse
- As of Visual Studio version 15.6 the Ctrl+D is no longer available per default and needs to be manually bound aswell
##### 1.2.14
- [x] Fixed behaviour: Saving the initial caret when Alt-clicking to add a second caret

##### 1.2.12
- [x] Added command ```ConvertSelectionToMultipleCursors``` - thanks @ leachdaniel

##### 1.2.9
- [x] Added command ```SelectAllOccurrences```

## 1.2
- [x] Switching command-prefix from ```Edit.``` to ```SelectNextOccurrence.```
- [x] Added command ```Add Caret Above```
- [x] Added command ```Add Caret Below```
- [x] Fixing multi copy/paste.
- [x] Better support for selecting right to left and turning.
- [x] Fixing removal of duplicate carets.
- [x] Adding extension icon.

##### 1.1.1(.1)
- [x] Fix for NullReference Exception when adding commandfilter

## 1.1
- [x] Added command ```Skip Occurrence```
- [x] Added command ```Undo Occurrence```
- [x] Fixed scrolling-bug that caused unwanted scrolling when navigating.
- [x] Better undoing if selections have been cleared.

##### 1.0.4
- [x] Changing scrolling-behaviour when selecting occurrences out of visible scope to use ViewScroller.EnsureSpanVisible
    This prevents unnecessary scrolling
- [x] Preventing some auto-scrolling when beginning to edit on multiple carets


##### 1.0.3
- [x] Toggle case-sensitive search by using the "Match case" setting from the find-dialog (Ctrl+F)

##### 1.0.2
- [x] Updated Nuget-packages
- [x] When selecting an occurence out of visible scope, editor now scrolls so the item is visible at the bottom instead of center to avoid screen-jumping when starting to edit

##### 1.0.1
- [x] Fixed ctrl+shift+home/end to release selections

## 1.0

- [x] Initial release
- [x] Select next occurrences
  - [x] Add caret with Alt-click
