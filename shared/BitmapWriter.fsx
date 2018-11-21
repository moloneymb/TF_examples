module BitmapWriter
// This is a lightweight library for saving bitmap files in order to view the output of image base machine learning
// The point is is have minimal dependencies

open System
open System.IO
open System
open System
open System

//https://en.wikipedia.org/wiki/BMP_file_format

let BGRAToBitmap(height:int, width:int, pixels:int[]) = 
    let header =
        [|
            0x42; 0x4D;                 // BM                           ID field
            0x00; 0x00; 0x00; 0x00;     //                              Size of the BMP file
            0x00; 0x00;                 // Unused
            0x00; 0x00;                 // Unused 
            0x7A; 0x00; 0x00; 0x00      // 122 bytes (14 + 108)         Offset where the pixel arrah can be found                 
            0x6C; 0x00; 0x00; 0x00      // 108 bytes                    Number of bytes in the DIB header (from this point)
            0x00; 0x00; 0x00; 0x00      //                              Width of the bitmap in pixels
            0x00; 0x00; 0x00; 0x00      //                              Height of the bitmap in pixels
            0x01; 0x00;                 //                              Number of color panes being used
            0x20; 0x00;                 // 32 bits                      Number of bits per pixel
            0x03; 0x00; 0x00; 0x00      // 1 plane                      BI_BITFIELDS, no pixel array compression used
            0x20; 0x00; 0x00; 0x00      // 32 bits                      Size of the rab bitmap data (including padding)
            0x13; 0x0b; 0x00; 0x00      // 2835 pixels/metre horizontal Print resolution of the image,
            0x13; 0x0b; 0x00; 0x00      // 2835 pixels/metre vertical   72 DPI 
            0x00; 0x00; 0x00; 0x00      // 0 colors                     Number of colors in the palette
            0x00; 0x00; 0x00; 0x00      // 0 important colors           0 means all colors are important
            0x00; 0x00; 0xFF; 0x00      // 00FF0000  in big-endian      Red channel bit mask
            0x00; 0xFF; 0x00; 0x00      // 0000FF00  in big-endian      Green channel bit mask
            0xFF; 0x00; 0x00; 0x00      // 000000FF  in big-endian      Blue channel bit mask
            0x00; 0x00; 0x00; 0xFF      // FF000000  in big-endian      Alpha channel bit mask
            0x20; 0x6E; 0x69; 0x57      // little-ending "Win "         LCS_WINDOWS_COLOR_SPACE
        |] |> Array.map byte
    // NOTE: CIEXYZTRIPLE are unused for LCS "Win " and are initialized to zero
    let sizeBmpFile = 122 + pixels.Length * 4
    let buffer = Array.zeroCreate<byte> sizeBmpFile
    Buffer.BlockCopy(header,0,buffer,0,74)
    [sizeBmpFile, 0x02; width, 0x12; height, 0x16] |> Seq.iter (fun (i,offset) ->  do Buffer.BlockCopy(BitConverter.GetBytes(i),0,buffer,offset,4))
    Buffer.BlockCopy(pixels,0,buffer,122,pixels.Length*4)
    buffer