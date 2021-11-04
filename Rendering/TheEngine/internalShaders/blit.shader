{
    "Pixel":
    {
        "Path": "blit.ps",
        "Entry" : "pixel"
    },
    "Vertex" :
    {
        "Path": "blit.vs",
        "Entry" : "vert",
        "Input" : [
            {
                "Type": "Float4",
                "Semantic" : "POSITION"
            },
            {
                "Type": "Float4",
                "Semantic": "COLOR0"
            },
            {
                "Type": "Float4",
                 "Semantic" : "NORMAL"
            },
            {
                "Type": "Float2",
                 "Semantic" : "TEXCOORD0"
            },
            {
                "Type": "Float2",
                 "Semantic" : "TEXCOORD1"
            }
        ]
    },
    "Textures": 1,
    "Instancing" : false,
	"ZWrite": false,
}