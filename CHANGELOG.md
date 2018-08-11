# Road map

- [x] Command to add caret above/below current caret (modifier(s) + Up/Down-key)
- [x] Command to convert a block-selection into carets
- [x] Command to select all occurrences
- [x] Undo last selection
- [x] Skip next occurrence (Sublime Ctrl+K, Ctrl+D)
- [ ] Make statement completion working on multiple selections, see [issue](https://github.com/2mas/SelectNextOccurrence/issues/5)
- [x] Add support for Visual Studio 2015

# Change log

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
