> WORK IN PROGRESS
>
> What you see here ever evolving notes and changing code as a investigate.

# Notepad-Tabstate-Buffer

These are my attempts to reverse engineer the Tabstate files for Notepad in Microsoft Windows 11. These files are located at: %localappdata%\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\LocalState\TabState

There are different types of .bin files that appear to save the state of the various tabs. These tabs could be:
- unsaved with text stored only in buffer
- saved file with unsaved changes stored only in buffer
- others?

For now, I will be focusing on getting a better understanding of the underlying structure for an unsaved tab with text.

## Known

 - First 17 bytes are header information (Ignoring for now. First 2 bytes do always appear to be "NP")
 - Proceeding these 17 bytes we have chunks of bytes that describe
   - Cursor position (Stored as a unsigned LEB128)
   - Action (1 byte)
   - Other information dependant on the action such as character inserted

## Unknown

 - Breakdown of the header information
 - Unknown bytes contained within the chunks. Particularly the last 5 bytes. These might be some sort of hash or CRC of the position and character/action?


