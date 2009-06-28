﻿Imports System.Drawing.Imaging
Imports System
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Drawing.Drawing2D
Imports FreeImageAPI
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Public Class ImageUtil

  Public Shared Function MakeGrayscale(ByVal original As System.Drawing.Bitmap) As System.Drawing.Bitmap
    'create a blank bitmap the same size as original
    Dim newBitmap As New System.Drawing.Bitmap(original.Width, original.Height)

    'get a graphics object from the new image
    Dim g As Graphics = Graphics.FromImage(newBitmap)

    'create the grayscale ColorMatrix
    Dim colorMatrix As New ColorMatrix(New Single()() {New Single() {0.3, 0.3, 0.3, 0, 0}, New Single() {0.59, 0.59, 0.59, 0, 0}, New Single() {0.11, 0.11, 0.11, 0, 0}, New Single() {0, 0, 0, 1, 0}, New Single() {0, 0, 0, 0, 1}})

    'create some image attributes
    Dim attributes As New ImageAttributes()

    'set the color matrix attribute
    attributes.SetColorMatrix(colorMatrix)

    'draw the original image on the new image
    'using the grayscale color matrix
    g.DrawImage(original, New Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height, _
     GraphicsUnit.Pixel, attributes)

    'dispose the Graphics object
    g.Dispose()

    Return newBitmap
  End Function

  Public Shared Function BitmapTo1Bpp(ByVal img As System.Drawing.Bitmap) As System.Drawing.Bitmap

    If img.PixelFormat <> PixelFormat.Format32bppPArgb Then
      Dim temp As New System.Drawing.Bitmap(img.Width, img.Height, PixelFormat.Format32bppPArgb)
      Dim g As Graphics = Graphics.FromImage(temp)
      g.DrawImage(img, New Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel)
      img.Dispose()
      g.Dispose()
      img = temp
    End If

    Dim imageTemp As Image
    imageTemp = img

    'lock the bits of the original bitmap
    Dim bmdo As BitmapData = img.LockBits(New Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat)

    'and the new 1bpp bitmap
    Dim bm As New System.Drawing.Bitmap(imageTemp.Width, imageTemp.Height, PixelFormat.Format1bppIndexed)
    Dim bmdn As BitmapData = bm.LockBits(New Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format1bppIndexed)

    'for diagnostics
    Dim dt As DateTime = DateTime.Now

    'scan through the pixels Y by X
    Dim y As Integer
    For y = 0 To img.Height - 1
      Dim x As Integer
      For x = 0 To img.Width - 1
        'generate the address of the colour pixel
        Dim index As Integer = y * bmdo.Stride + x * 4
        'check its brightness
        If Color.FromArgb(Marshal.ReadByte(bmdo.Scan0, index + 2), Marshal.ReadByte(bmdo.Scan0, index + 1), Marshal.ReadByte(bmdo.Scan0, index)).GetBrightness() > 0.5F Then
          Dim imgUtil As New ImageUtil
          imgUtil.SetIndexedPixel(x, y, bmdn, True) 'set it if its bright.
        End If
      Next x
    Next y
    'tidy up
    bm.UnlockBits(bmdn)
    img.UnlockBits(bmdo)
    imageTemp = Nothing
    'display the 1bpp image.
    Return bm
    End Function

    Public Shared Sub RotateImageClockwise(ByRef pPicBox As PictureBox)
        pPicBox.Image.RotateFlip(RotateFlipType.Rotate90FlipNone)
        pPicBox.Refresh()
    End Sub

    Public Shared Sub RotateImageCounterclockwise(ByRef pPicBox As PictureBox)
        pPicBox.Image.RotateFlip(RotateFlipType.Rotate270FlipNone)
        pPicBox.Refresh()
    End Sub

    Public Shared Sub PictureBoxZoomActual(ByRef pPicBox As PictureBox)
        pPicBox.Width = pPicBox.Image.Width
        pPicBox.Height = pPicBox.Image.Height
        pPicBox.SizeMode = PictureBoxSizeMode.Zoom
    End Sub

    Public Shared Sub PictureBoxZoomPageWidth(ByRef pPicBox As PictureBox)
        pPicBox.Width = pPicBox.Parent.ClientSize.Width - 18
        Dim ScaleAmount As Double = (pPicBox.Width / pPicBox.Image.Width)
        pPicBox.Height = CInt(pPicBox.Image.Height * ScaleAmount)
        pPicBox.SizeMode = PictureBoxSizeMode.Zoom
    End Sub

    Public Shared Sub PictureBoxZoomFit(ByRef pPicBox As PictureBox)
        pPicBox.Height = pPicBox.Parent.ClientSize.Height - 7
        pPicBox.Width = pPicBox.Parent.ClientSize.Width - 7
        pPicBox.SizeMode = PictureBoxSizeMode.Zoom
        pPicBox.Location = New Point(0, 0)
    End Sub

    Public Shared Sub PictureBoxZoomIn(ByRef pPicBox As PictureBox)
        pPicBox.SizeMode = PictureBoxSizeMode.Zoom
        pPicBox.Width = CInt(pPicBox.Width * 1.25)
        pPicBox.Height = CInt(pPicBox.Height * 1.25)
        pPicBox.Refresh()
    End Sub

    Public Shared Sub PictureBoxZoomOut(ByRef pPicBox As PictureBox)
        pPicBox.SizeMode = PictureBoxSizeMode.Zoom
        pPicBox.Width = CInt(pPicBox.Width / 1.25)
        pPicBox.Height = CInt(pPicBox.Height / 1.25)
        pPicBox.Refresh()
    End Sub

    Public Shared Function GenerateThumbnail(ByVal original As Image, ByVal percentage As Integer) As Image
        If percentage < 1 Then
            Throw New Exception("Thumbnail size must be aat least 1% of the original size")
        End If
        Dim tn As New System.Drawing.Bitmap(CInt(original.Width * 0.01F * percentage), CInt(original.Height * 0.01F * percentage))
        Dim g As Graphics = Graphics.FromImage(tn)
        g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBilinear
        g.DrawImage(original, New Rectangle(0, 0, tn.Width, tn.Height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel)
        g.Dispose()
        Return CType(tn, Image)
    End Function

    Public Shared Function SaveImageToTiff(ByVal img As Image) As String
        Dim sTemp As String = My.Computer.FileSystem.SpecialDirectories.Temp & "\DDI_"
        Dim sTStamp As String = Format(Now, "yyyyMMddhhmmssfff")
        Dim FileName As String = sTemp & sTStamp & ".tif"
        img.Save(FileName, ImageFormat.Tiff)
        Return FileName
    End Function

    Public Shared Function GetFrameFromTiff(ByVal Filename As String, ByVal FrameNumber As Integer) As Image
        Dim fs As FileStream = File.Open(Filename, FileMode.Open, FileAccess.Read)
        Dim bm As System.Drawing.Bitmap = CType(System.Drawing.Bitmap.FromStream(fs), System.Drawing.Bitmap)
        bm.SelectActiveFrame(FrameDimension.Page, FrameNumber)
        Dim temp As New System.Drawing.Bitmap(bm.Width, bm.Height)
        Dim g As Graphics = Graphics.FromImage(temp)
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        g.DrawImage(bm, 0, 0, bm.Width, bm.Height)
        g.Dispose()
        GetFrameFromTiff = temp
        fs.Close()
    End Function

    Public Shared Function GetFrameFromTiff2(ByVal Filename As String, ByVal FrameNumber As Integer) As Image
        Dim dib As FIMULTIBITMAP = New FIMULTIBITMAP()
        dib = FreeImage.OpenMultiBitmapEx(Filename)
        Dim page As FIBITMAP = New FIBITMAP()
        page = FreeImage.LockPage(dib, FrameNumber)
        GetFrameFromTiff2 = FreeImage.GetBitmap(page)
        page.SetNull()
        FreeImage.CloseMultiBitmapEx(dib)
    End Function

    Public Shared Function GetImageFrameFromFileForPrint(ByVal sFileName As String, ByVal iFrameNumber As Integer) As Image
        If ImageUtil.IsPDF(sFileName) Then 'convert one frame to a tiff for viewing
            sFileName = ConvertPDF.PDFConvert.ConvertPdfToTiff(sFileName, iFrameNumber + 1, True)
            GetImageFrameFromFileForPrint = ImageUtil.GetFrameFromTiff2(sFileName, 0)
            ImageUtil.DeleteFile(sFileName)
        ElseIf ImageUtil.IsTiff(sFileName) Then
            GetImageFrameFromFileForPrint = ImageUtil.GetFrameFromTiff2(sFileName, iFrameNumber)
        End If
    End Function

    Public Shared Function GetImageFrameCount(ByVal sFileName As String) As Integer
        If ImageUtil.IsPDF(sFileName) Then
            GetImageFrameCount = iTextSharpUtil.GetPDFPageCount(sFileName)
        ElseIf ImageUtil.IsTiff(sFileName) Then
            Dim dib As FIMULTIBITMAP = New FIMULTIBITMAP()
            dib = FreeImage.OpenMultiBitmapEx(sFileName)
            GetImageFrameCount = FreeImage.GetPageCount(dib)
            FreeImage.CloseMultiBitmapEx(dib)
        End If
    End Function

    Public Shared Function GetTiffFrameCount(ByVal FileName As String) As Integer
        Dim fs As FileStream = File.Open(FileName, FileMode.Open, FileAccess.Read)
        Dim bm As System.Drawing.Bitmap = CType(System.Drawing.Bitmap.FromStream(fs), System.Drawing.Bitmap)
        GetTiffFrameCount = bm.GetFrameCount(FrameDimension.Page)
        fs.Close()
    End Function

    Public Shared Sub DeleteFile(ByVal filename As String)
        Try
            System.IO.File.Delete(filename)
        Catch ex As Exception
        End Try
    End Sub

    Public Shared Function IsTiff(ByVal filename As String) As Boolean
        If Nothing Is filename Then Return False
        Return Regex.IsMatch(filename, "\.tiff*$", RegexOptions.IgnoreCase)
    End Function

    Public Shared Function IsPDF(ByVal filename As String) As Boolean
        If Nothing Is filename Then Return False
        Return Regex.IsMatch(filename, "\.pdf$", RegexOptions.IgnoreCase)
    End Function

    Public Shared Function CropBitmap(ByRef bmp As System.Drawing.Bitmap, ByVal cropX As Integer, ByVal cropY As Integer, ByVal cropWidth As Integer, ByVal cropHeight As Integer) As System.Drawing.Bitmap
        Dim rect As New Rectangle(cropX, cropY, cropWidth, cropHeight)
        Dim cropped As System.Drawing.Bitmap = bmp.Clone(rect, bmp.PixelFormat)
        Return cropped
    End Function

    'Needed subroutine for 1bit conversion
    Protected Sub SetIndexedPixel(ByVal x As Integer, ByVal y As Integer, ByVal bmd As BitmapData, ByVal pixel As Boolean)
        Dim index As Integer = y * bmd.Stride + (x >> 3)
        Dim p As Byte = Marshal.ReadByte(bmd.Scan0, index)
        Dim mask As Byte = &H80 >> (x And &H7)
        If pixel Then
            p = p Or mask
        Else
            p = p And CByte(mask ^ &HFF)
        End If
        Marshal.WriteByte(bmd.Scan0, index, p)
    End Sub

End Class
