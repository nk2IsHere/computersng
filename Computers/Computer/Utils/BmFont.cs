using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#pragma warning disable CS8618

namespace Computers.Computer.Utils;

public class BmFont {
    
    private readonly Dictionary<char, FontChar> _characterMap;
    private readonly Texture2D _texture;
    private readonly FontInfo _info;
    
    private readonly float _maxScale;
    
    private readonly Color[] _materializedTexture;
    private readonly Color[] _materializedScaledTexture;
    
    public static BmFont? Load(string fontDefinition, Texture2D fontTexture) {
        var deserializer = new XmlSerializer(typeof(FontFile));
        var file = (FontFile?) deserializer.Deserialize(new StringReader(fontDefinition));
        
        return file != null 
            ? new BmFont(file, fontTexture) 
            : null;
    }

    private BmFont(FontFile fontFile, Texture2D fontTexture) {
        _info = fontFile.Info;
        
        _texture = fontTexture;
        _characterMap = new Dictionary<char, FontChar>();

        foreach (var fontCharacter in fontFile.Chars) {
            var c = (char)fontCharacter.Id;
            _characterMap.Add(c, fontCharacter);
        }
        
        _materializedTexture = new Color[fontTexture.Width * fontTexture.Height];
        fontTexture.GetData(0, null, _materializedTexture, 0, _materializedTexture.Length);
        
        _maxScale = 3.0f;
        _materializedScaledTexture = new Color[fontTexture.Width * fontTexture.Height * (int) (_maxScale * _maxScale)];
    }
    
    public (int, int) Measure(string text, float scale) {
        var width = 0;
        var height = 0;
        foreach (var c in text) {
            if (!_characterMap.TryGetValue(c, out var fc)) {
                continue;
            }
            width += (int) (fc.XAdvance * scale);
            height = Math.Max(height, (int)(fc.Height * scale));
        }
        return (width, height);
    }
    
    public (int, int) MeasureGlyph(char c, float scale) {
        return _characterMap.TryGetValue(c, out var fc) 
            ? ((int) (fc.XAdvance * scale), (int) (fc.Height * scale)) 
            : (0, 0);
    }
    
    public int GlyphSize() {
        return _info.Size;
    }
    
    public float MaxScale() {
        return _maxScale;
    }

    public void Draw(SpriteBatch spriteBatch, int x, int y, string text, float scale, Color color) {
        scale = Math.Min(scale, _maxScale);
        
        var dx = x;
        foreach (var c in text) {
            if (!_characterMap.TryGetValue(c, out var fc)) {
                continue;
            }
            
            var sourceRectangle = new Rectangle(fc.X, fc.Y, fc.Width, fc.Height);
            var destinationRectangle = new Rectangle(
                (int) (dx + fc.XOffset * scale),
                (int) (y + fc.YOffset * scale), // Y never changes
                (int) (fc.Width * scale),
                (int) (fc.Height * scale)
            );

            spriteBatch.Draw(_texture, destinationRectangle, sourceRectangle, color);
            dx += (int) (fc.XAdvance * scale);
        }
    }
    
    public void Draw(Color[] colorData, int canvasWidth, int canvasHeight, int x, int y, string text, float scale, Color color) {
        scale = Math.Min(scale, _maxScale);
        var dx = x;
        
        foreach (var c in text) {
            if (!_characterMap.TryGetValue(c, out var fc)) {
                continue;
            }
            
            var scaledWidth = (int) (fc.Width * scale);
            var scaledHeight = (int) (fc.Height * scale);
            var scaledLength = scaledWidth * scaledHeight;
            
            // Scale the texture data using nearest neighbor
            for (var i = 0; i < scaledLength; i++) {
                var row = i / scaledWidth;
                var col = i % scaledWidth;
                
                var dataRow = (int) (row / scale);
                var dataCol = (int) (col / scale);
                
                var index = fc.Y * _texture.Width + fc.X + dataRow * _texture.Width + dataCol;
                
                var originalColor = _materializedTexture[index];
                var properColor = new Color(color.R, color.G, color.B, originalColor.A);

                _materializedScaledTexture[i] = properColor;
            }
            
            // Draw the scaled texture data
            for (var i = 0; i < scaledLength; i++) {
                var row = i / scaledWidth;
                var col = i % scaledWidth;
                
                var dataRow = y + row; // Y never changes
                var dataCol = dx + col;
                
                if (dataRow < 0 || dataRow >= canvasHeight || dataCol < 0 || dataCol >= canvasWidth) {
                    continue;
                }

                var index = dataRow * canvasWidth + dataCol;
                colorData[index] = Color.Lerp(colorData[index], _materializedScaledTexture[i], _materializedScaledTexture[i].A / 255f);
            }
            
            dx += (int) (fc.XAdvance * scale);
        }
    }
}

[Serializable]
[XmlRoot("font")]
public class FontFile {
    [XmlElement("info")] public FontInfo Info { get; set; }

    [XmlElement("common")] public FontCommon Common { get; set; }

    [XmlArray("pages")]
    [XmlArrayItem("page")]
    public List<FontPage> Pages { get; set; }

    [XmlArray("chars")]
    [XmlArrayItem("char")]
    public List<FontChar> Chars { get; set; }

    [XmlArray("kernings")]
    [XmlArrayItem("kerning")]
    public List<FontKerning> Kernings { get; set; }
}

[Serializable]
public class FontInfo {
    [XmlAttribute("face")] public string Face { get; set; }

    [XmlAttribute("size")] public int Size { get; set; }

    [XmlAttribute("bold")] public int Bold { get; set; }

    [XmlAttribute("italic")] public int Italic { get; set; }

    [XmlAttribute("charset")] public string CharSet { get; set; }

    [XmlAttribute("unicode")] public int Unicode { get; set; }

    [XmlAttribute("stretchH")] public int StretchHeight { get; set; }

    [XmlAttribute("smooth")] public int Smooth { get; set; }

    [XmlAttribute("aa")] public int SuperSampling { get; set; }

    private Rectangle _padding;

    [XmlAttribute("padding")]
    public string Padding {
        get => _padding.X + "," + _padding.Y + "," + _padding.Width + "," + _padding.Height;
        set {
            var padding = value.Split(',');
            _padding = new Rectangle(
                Convert.ToInt32(padding[0]), Convert.ToInt32(padding[1]), Convert.ToInt32(padding[2]),
                Convert.ToInt32(padding[3])
            );
        }
    }

    private Point _spacing;

    [XmlAttribute("spacing")]
    public string Spacing {
        get => _spacing.X + "," + _spacing.Y;
        set {
            var spacing = value.Split(',');
            _spacing = new Point(Convert.ToInt32(spacing[0]), Convert.ToInt32(spacing[1]));
        }
    }

    [XmlAttribute("outline")] public int OutLine { get; set; }
}

[Serializable]
public class FontCommon {
    [XmlAttribute("lineHeight")] public int LineHeight { get; set; }

    [XmlAttribute("base")] public int Base { get; set; }

    [XmlAttribute("scaleW")] public int ScaleW { get; set; }

    [XmlAttribute("scaleH")] public int ScaleH { get; set; }

    [XmlAttribute("pages")] public int Pages { get; set; }

    [XmlAttribute("packed")] public int Packed { get; set; }

    [XmlAttribute("alphaChnl")] public int AlphaChannel { get; set; }

    [XmlAttribute("redChnl")] public int RedChannel { get; set; }

    [XmlAttribute("greenChnl")] public int GreenChannel { get; set; }

    [XmlAttribute("blueChnl")] public int BlueChannel { get; set; }
}

[Serializable]
public class FontPage {
    [XmlAttribute("id")] public int Id { get; set; }

    [XmlAttribute("file")] public string File { get; set; }
}

[Serializable]
public class FontChar {
    [XmlAttribute("id")] public int Id { get; set; }

    [XmlAttribute("x")] public int X { get; set; }

    [XmlAttribute("y")] public int Y { get; set; }

    [XmlAttribute("width")] public int Width { get; set; }

    [XmlAttribute("height")] public int Height { get; set; }

    [XmlAttribute("xoffset")] public int XOffset { get; set; }

    [XmlAttribute("yoffset")] public int YOffset { get; set; }

    [XmlAttribute("xadvance")] public int XAdvance { get; set; }

    [XmlAttribute("page")] public int Page { get; set; }

    [XmlAttribute("chnl")] public int Channel { get; set; }
}

[Serializable]
public class FontKerning {
    [XmlAttribute("first")] public int First { get; set; }

    [XmlAttribute("second")] public int Second { get; set; }

    [XmlAttribute("amount")] public int Amount { get; set; }
}
