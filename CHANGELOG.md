# Road map

- [x] Command to add caret above/below current caret (modifier(s) + Up/Down-key)
- [ ] Command to convert a block-selection into carets
- [ ] Command to select all occurrences
- [x] Undo last selection
- [x] Skip next occurrence (Sublime Ctrl+K, Ctrl+D)

# Change log

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

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