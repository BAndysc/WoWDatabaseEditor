{
    "Pixel":
    {
        "Path": "unlit.ps",
        "Entry" : "pixel"
    },
    "Vertex" :
    {
        "Path": "unlit.vs",
        "Entry" : "vert",
        "Input" : [
            {
                "Type": "Float4",
                "Semantic" : "POSITION"
            },
			{
				"Type": "Float4",
				"Semantic" : "COLOR0"
			},
			{
				"Type": "Float4",
				 "Semantic" : "NORMAL"
			},
			{
				"Type": "Float2",
				 "Semantic" : "TEXCOORD0"
			}
        ]
    },
    "Textures": 1,
    "Instancing" : true,
	"ZWrite": true,
}