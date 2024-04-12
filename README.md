> [!WARNING]  
> WORK IN PROGRESS
>
> What you see here are ever evolving notes and changing code as I investigate the file format.

# Notepad-Tabstate-Buffer

These are my attempts to reverse engineer the Tabstate files for Notepad in Microsoft Windows 11.. These files are located at: `%localappdata%\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\LocalState\TabState`

There are different types of .bin files that appear to save the state of the various tabs. These tabs could be:
- unsaved with text stored only in buffer
- saved file with unsaved changes stored only in buffer
- others?

For now, I will be focusing on getting a better understanding of the underlying structure for a new unsaved tab with text. I have not attacked the 0.bin and 1.bin files.

Please see my other repository for the [Windowstate files](https://github.com/ogmini/Notepad-Windowstate-Buffer/).

# Acknowledgements

[NordGaren](https://github.com/Nordgaren) for the inspiration to take a look at this when I saw his [tabstate-util](https://github.com/Nordgaren/tabstate-util)     
[jlogsdon](https://github.com/jlogsdon) for lots of help  
[JustArion](https://github.com/JustArion) for lots of help  


## Overall Behavior

- Opening Notepad with no existing tab(s) will create an empty "Untitled" tab with its associated bin.
- Opening a file will create an associated bin.
- Closing a specific tab within Notepad will delete the associated bin file(s).  
- Closing Notepad
  - New tab with no changes will create an associated bin, 0.bin, and 1.bin.
  - New tab with unsaved changes will create an associated bin.
    - On subsequent open/close of Notepad it will create an associated bin, 0.bin, and 1.bin. (Existing Tab Behavior) 
  - Opened file tab with no changes will create an associated bin
    - On subsequent open/close of Notepad it will create an associated bin, 0.bin, and 1.bin. (Existing Tab Behavior)  
  - Opened file tab with unsaved changes will create an associated bin.
    - On subsequent open/close of Notepad it will create an associated bin, 0.bin, and 1.bin. (Existing Tab Behavior)
  - Existing tab fi viewed will create an associated bin, 0.bin, and 1.bin.
- No bin files indicate having never opened Notepad or closed all tab(s) manually.

> WORK IN PROGRESS

 - When/Why do 0.bin and 1.bin get created?
 - When/Why do 0.bin and 1.bin get deleted?
 - What causes a flush of the Unsaved Buffer?

## File Format

There appear to be two slightly different file formats for File Tabs and Unsaved Tabs.

### File Tab

 - First 2 bytes are "NP"
 - 3rd byte is unknown
   - Possibly a NULL as a delimiter
   - Maybe Sequence number (Stored as a uLEB128)(Always 00)
 - 4th byte appears to be flag for saved file
 - Length of Filepath (Stored as a uLEB128)
 - Filepath as little-endian UTF-16
 - Length of original content (Stored as a uLEB128)
 - Encoding
 - Carriage Return Type
 - Timestamp stored as a uLEB128
 - SHA256 of the original saved file
 - Delimiter of 00 01?
 - Selection Start Index on save (Stored as a uLEB128)
   - I don't think this will extend to the Unsaved tab as this seems to only show up on Save 
 - Selection End Index on save (Stored as a uLEB128) 
   - I don't think this will extend to the Unsaved tab as this seems to only show up on Save     
 - Delimiter? 01 00 00 00 (Appears to be WordWrap , Right to Left, Show Unicode, Unknown)
 - Length of original content (Stored as a uLEB128)
 - Content
 - Unknown 1 byte
   - Possibly a NULL as a delimiter
 - CRC 32 of all the previous bytes starting from the 3rd byte 
 - Unsaved Buffer Chunks

### Unsaved Tab

 - First 2 bytes are "NP"
 - 3rd byte is unknown
   - Possibly a NULL as a delimiter
   - Maybe Sequence number (Stored as a uLEB128)(Always 00)
 - 4th byte appears to be flag for unsaved tab
 - Always 01? Is this also length of Filepath like above?
 - Length of original content (Stored as a uLEB128)
 - Length of original content AGAIN (Stored as a uLEB128)
 - Delimiter? 01 00 00 00 ((Appears to be WordWrap , Right to Left, Show Unicode, Unknown - But this doesn't change. Changes do appear in the 0.bin and 1.bin) 
 - Length of original content AGAIN (Stored as a uLEB128)
 - Content
 - Unknown 1 byte
   - Possibly a NULL as a delimiter
 - CRC 32 of all the previous bytes starting from the 3rd byte 
 - Unsaved Buffer Chunks

### 0.bin / 1.bin

- First 2 bytes are "NP"
- Sequence number (Stored as a uLEB128)
- 4th number of bytes until the CRC at the end (What is this stored as? uLEB128? int? )
- Delimiter? 0x00
- Size of the associated BIN File in bytes (Stored as a uLEB128)
- Selection Start Index on close (Stored as a uLEB128)  
- Selection End Index on close (Stored as a uLEB128)   
- Delimiter? 01 00 00 00 (Appears to be WordWrap , Right to Left, Show Unicode, Unknown)
- CRC 32 of all the previous bytes starting from the 3rd byte

## Chunk Format for Unsaved Buffer

[Cursor Position][Deletion][Addition][CRC32]
- Cursor position (Stored as a uLEB128)
- Deletion Action (Stored as a uLEB128 indicating how many characters to delete)
- Addition Action (Stored as a uLEB128 indicating how many characters to add)
  - Added characters are stored as little-endian UTF-16
- CRC 32 of the previous bytes
  
### Addition Chunk

Below is an example of a chunk of bytes that represent the addition of the character 'a' at position 0.

![Screenshot of Insertion](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/Insert-Chunk.png)

00 - unsigned LEB128 denoting position of 0  
00 - unsigned LEB128 denoting number of characters deleted  
01 - unsigned LEB128 denoting number of characters added (In this case 1)     
61 00 - character 'a'  
BB 06 C7 CC - CRC 32 of previous bytes  

Below is an example of a chunk of bytes that represent the addition of the character 'a' at position 17018.

![Screenshot of Insertion](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/Insert-Chunk-2.png)
FA 84 01 - unsigned LEB128 denoting position of 17018  
00 - unsigned LEB128 denoting number of characters deleted   
01 - unsigned LEB128 denoting number of characters added (In this case 1)         
61 00 - character 'a'  
98 07 F5 46 - CRC 32 of previous bytes     

### Deletion Chunk 

Below is an example of a chunk of bytes that represent deletion at a position 1.

![Screenshot of Deletion](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/Delete-Chunk.png)
01 - unsigned LEB128 denoting position of 1  
01 - unsigned LEB128 denoting number of characters deleted (In this case 1)      
00 - unsigned LEB128 denoting number of characters added   
E7 98 82 64 - CRC 32 of previous bytes 

### Insertion Chunk

Insertion chunk is a combination of the addition and deletion. This would occur if someone was to select text and paste new text into place. Below is an example of a chunk of bytes that represent pasting 3 character 'b's over 3 character 'a's starting at position 1.

![Screenshot of Insertion](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Insertion%20Chunk.png)

01 - unsigned LEB128 denoting position 1  
03 - unsigned LEB128 denoting the number of characters deleted (In this case 3)  
03 - unsigned LEB128 denoting the number of characters added  
62 00 - character 'b'  
62 00 - character 'b'  
62 00 - character 'b'  
CD CD 85 6F - CRC 32 of previous bytes 

## Open Questions

 - What is up with the weird delimiter in Unsaved Tab Format?
 - What are the 43 unknown bytes in the File Tab Format?
 - Why are Unsaved Tab and File Tab Format different?
 - Random single bytes?
 - Are the 0.bin and 1.bin just backups?
 - What other actions are there?

## Application

> WORK IN PROGRESS

Expect this to change drastically over time. A lot is hardcoded and messy. You have been warned. Some things I would like to implement.

 - Some visual playback of actions taken from the unsaved buffer

![Example 1-1](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-1.png)
![Example 1-2](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-2.png)
![Example 1-3](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-3.png)
![Example 1-4](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-4.png)
