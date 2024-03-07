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

# Acknowledgements

[NordGaren](https://github.com/Nordgaren) for the inspiration to take a look at this when I saw his [tabstate-util](https://github.com/Nordgaren/tabstate-util)   
[jlogsdon](https://github.com/jlogsdon) for lots of help and suggestions
[JustArion](https://github.com/JustArion) for pointing out the Selection Index 


## Overall Behavior

> WORK IN PROGRESS

 - Why do .bin files get deleted?
 - When do 0.bin and 1.bin get created?
 - Why do 0.bin and 1.bin get created?
 - What causes a flush of the Unsaved Buffer?

## File Format

There appear to be two slightly different file formats for File Tabs and Unsaved Tabs.

### File Tab

 - First 2 bytes are "NP"
 - 3rd byte is unknown
   - Possibly a NULL as a delimiter
 - 4th byte appears to be flag for saved file
 - Length of Filepath (Stored as an unsigned LEB128)
 - Filepath as little-ending UTF-16
 - Length of original content (Stored as an unsigned LEB128)
 - 43 Unknown Bytes (Seemingly starting with 05 01)
 - Delimiter of 00 01?
 - Selection Start Index on save (Stored as an unsigned LEB128) (Thanks [JustArion](https://github.com/JustArion) for pointing this out)
   - I don't think this will extend to the Unsaved tab as this seems to only show up on Save 
 - Selection End Index on save (Stored as an unsigned LEB128) (Thanks [JustArion](https://github.com/JustArion) for pointing this out)
   - I don't think this will extend to the Unsaved tab as this seems to only show up on Save     
 - Delimiter of 01 00 00 00?
 - Length of original content (Stored as an unsigned LEB128)
 - Content
 - Unknown 1 byte
   - Possibly a NULL as a delimiter
 - CRC 32 of all the previous bytes starting from the 3rd byte 
 - Unsaved Buffer Chunks

### Unsaved Tab

 - First 2 bytes are "NP"
 - 3rd byte is unknown
   - Possibly a NULL as a delimiter
 - 4th byte appears to be flag for saved file
 - Always 01? Is this also length of Filepath like above?
 - Length of original content (Stored as an unsigned LEB128)
 - Length of original content AGAIN (Stored as an unsigned LEB128)
 - Followed by 01 00 00 00
   - Random Bytes
 - Length of original content AGAIN (Stored as an unsigned LEB128)
 - Content
 - Unknown 1 byte
   - Possibly a NULL as a delimiter
 - CRC 32 of all the previous bytes starting from the 3rd byte 
 - Unsaved Buffer Chunks

### 0.bin / 1.bin

- First 2 bytes are "NP"
- 3rd byte might reference the bin number. (00 for 0.bin and 01 for 1.bin)
- 4th byte 09 (Need more testing)
- 14 Unknown Bytes
- CRC 32 of all the previous bytes starting from the 3rd byte

## Chunk Format for Unsaved Buffer

[Cursor Position][Deletion][Addition][CRC32]
- Cursor position (Stored as a unsigned LEB128)
- Deletion Action (Stored as an unsigned LEB128 indicating how many characters to delete)
- Addition Action (Stored as an unsigned LEB128 indicating how many characters to add)
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
