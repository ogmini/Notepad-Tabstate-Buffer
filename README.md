> WORK IN PROGRESS
>
> What you see here are ever evolving notes and changing code as I investigate the file format.

# Notepad-Tabstate-Buffer

These are my attempts to reverse engineer the Tabstate files for Notepad in Microsoft Windows 11.. These files are located at: `%localappdata%\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\LocalState\TabState`

There are different types of .bin files that appear to save the state of the various tabs. These tabs could be:
- unsaved with text stored only in buffer
- saved file with unsaved changes stored only in buffer
- others?

For now, I will be focusing on getting a better understanding of the underlying structure for a new unsaved tab with text.

## Header

 - First 2 bytes are "NP"
 - 3rd byte is unknown
 - 4th byte appears to be flag for saved file
 - Length of Filepath (Stored as an unsigned LEB128)
 - Filepath as little-ending UTF-16
 - Length of original content (Stored as an unsigned LEB128)
 - Unknown possibly 52 bytes (Need to investigate)
 - Content
 - Unknown 5 bytes  

## Chunk Format for Unsaved Buffer

[Cursor Position][Deletion][Addition][Unknown]
- Cursor position (Stored as a unsigned LEB128)
- Deletion Action (Stored as an unsigned LEB128 indicating how many characters to delete)
- Addition Action (Stored as an unsigned LEB128 indicating how many characters to add)
  - Added characters are stored as little-endian UTF-16 
  
### Addition Chunk

Below is an example of a chunk of bytes that represent the addition of the character 'a' at position 0.

![Screenshot of Insertion](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/Insert-Chunk.png)

00 - unsigned LEB128 denoting position of 0  
00 - unsigned LEB128 denoting number of characters deleted  
01 - unsigned LEB128 denoting number of characters added (In this case 1)     
61 00 - character 'a'  
BB 06 C7 CC - Unknown, possibly a hash/CRC of the position and character?  

Below is an example of a chunk of bytes that represent the addition of the character 'a' at position 17018.

![Screenshot of Insertion](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/Insert-Chunk-2.png)
FA 84 01 - unsigned LEB128 denoting position of 17018  
00 - unsigned LEB128 denoting number of characters deleted   
01 - unsigned LEB128 denoting number of characters added (In this case 1)         
61 00 - character 'a'  
98 07 F5 46 - Unknown, possibly a hash/CRC of the position and character?  

### Deletion Chunk 

Below is an example of a chunk of bytes that represent deletion at a position 1.

![Screenshot of Deletion](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/Delete-Chunk.png)
01 - unsigned LEB128 denoting position of 1  
01 - unsigned LEB128 denoting number of characters deleted (In this case 1)      
00 - unsigned LEB128 denoting number of characters added   
E7 98 82 64 - Unknown, possibly a hash/CRC of the position and action? Interesting this is now 4 bytes

### Insertion Chunk

## Open Questions

 - Breakdown of the header information
 - Unknown bytes contained within the chunks. Particularly the last 4 bytes. These might be some sort of hash or CRC of the position and character/action?
 - What other actions are there?

## Application

> WORK IN PROGRESS

At the moment, this will only work on deciphering unsaved buffers. Expect this to change drastically over time. A lot is hardcoded and messy. You have been warned.

![Example 1-1](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-1.png)
![Example 1-2](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-2.png)
![Example 1-3](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-3.png)
![Example 1-4](https://github.com/ogmini/Notepad-Tabstate-Buffer/blob/main/screenshots/Example%201-4.png)
