# Render the original vector geometry into Windows icon sizes. No external artwork.
Add-Type -AssemblyName System.Drawing
$assets=Join-Path $PSScriptRoot '../src/Zen3D.Tuner/Assets'
$images=@()
foreach($size in @(16,24,32,48,64,128,256)) {
 $bitmap=[Drawing.Bitmap]::new($size,$size)
 $g=[Drawing.Graphics]::FromImage($bitmap)
 $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $g.ScaleTransform($size/256.0,$size/256.0)
 $bg=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#101923'))
 $mint=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#54DFC3'),10)
 $mint.StartCap=$mint.EndCap=[Drawing.Drawing2D.LineCap]::Round
 $inner=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#192C37'))
 $white=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#E4FFF8'))
 function RoundRect($x,$y,$w,$h,$r) {
  $p=[Drawing.Drawing2D.GraphicsPath]::new()
  $p.AddArc($x,$y,$r*2,$r*2,180,90); $p.AddArc($x+$w-2*$r,$y,$r*2,$r*2,270,90)
  $p.AddArc($x+$w-2*$r,$y+$h-2*$r,$r*2,$r*2,0,90); $p.AddArc($x,$y+$h-2*$r,$r*2,$r*2,90,90)
  $p.CloseFigure(); return $p
 }
 $outer=RoundRect 8 8 240 240 56; $g.FillPath($bg,$outer)
 foreach($v in @(88,128,168)) { $g.DrawLine($mint,$v,37,$v,59); $g.DrawLine($mint,$v,197,$v,219); $g.DrawLine($mint,37,$v,59,$v); $g.DrawLine($mint,197,$v,219,$v) }
 $chip=RoundRect 61 61 134 134 24; $g.FillPath($inner,$chip); $g.DrawPath($mint,$chip)
 $points=[Drawing.PointF[]]@([Drawing.PointF]::new(87,88),[Drawing.PointF]::new(169,88),[Drawing.PointF]::new(169,106),[Drawing.PointF]::new(115,150),[Drawing.PointF]::new(169,150),[Drawing.PointF]::new(169,168),[Drawing.PointF]::new(87,168),[Drawing.PointF]::new(87,150),[Drawing.PointF]::new(141,106),[Drawing.PointF]::new(87,106))
 $g.FillPolygon($white,$points); $g.FillEllipse($mint.Brush,182,46,28,28)
 $stream=[IO.MemoryStream]::new(); $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
 $images+=,@{Size=$size;Bytes=$stream.ToArray()}
 if($size -eq 256) { $bitmap.Save((Join-Path $assets 'zen3d.png'),[Drawing.Imaging.ImageFormat]::Png) }
 $stream.Dispose(); $outer.Dispose(); $chip.Dispose(); $bg.Dispose(); $mint.Dispose(); $inner.Dispose(); $white.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
$writer=[IO.BinaryWriter]::new([IO.File]::Create((Join-Path $assets 'zen3d.ico')))
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$images.Count)
$offset=6+16*$images.Count
foreach($img in $images) {
 $dimension=if($img.Size -eq 256){0}else{$img.Size}
 $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
 $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$img.Bytes.Length); $writer.Write([uint32]$offset)
 $offset+=$img.Bytes.Length
}
foreach($img in $images){$writer.Write([byte[]]$img.Bytes)}
$writer.Dispose()
