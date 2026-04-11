using Veldrid;

namespace AkiGames.Core
{
    public class SpriteBatch : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly CommandList _cl;
        private readonly Pipeline _pipeline;
        private readonly ResourceLayout _resourceLayout;
        private readonly DeviceBuffer _vertexBuffer;
        private readonly DeviceBuffer _indexBuffer;
        private readonly Dictionary<Texture, ResourceSet> _resourceSets = [];

        private const int MaxQuads = 4096;
        private readonly ushort[] _indices;
        private List<Batch> _batches = [];
        private Batch? _currentBatch;

        private struct Batch
        {
            public Texture Texture;
            public List<float> Vertices;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public SpriteBatch(GraphicsDevice gd, CommandList cl)
        {
            _gd = gd;
            _cl = cl;
            GraphicsDevice = gd;

            byte[] vsBytes = File.ReadAllBytes("Content/Shaders/SpriteBatch.vert.spv");
            byte[] fsBytes = File.ReadAllBytes("Content/Shaders/SpriteBatch.frag.spv");

            var vs = gd.ResourceFactory.CreateShader(new ShaderDescription(
                ShaderStages.Vertex, vsBytes, "main"));
            var fs = gd.ResourceFactory.CreateShader(new ShaderDescription(
                ShaderStages.Fragment, fsBytes, "main"));

            _resourceLayout = gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("_Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("_Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));

            var pipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    shaders: [vs, fs],
                    vertexLayouts: [
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4)
                        )
                    ]
                ),
                [_resourceLayout],
                gd.SwapchainFramebuffer.OutputDescription
            );
            _pipeline = gd.ResourceFactory.CreateGraphicsPipeline(pipelineDesc);

            _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(MaxQuads * 4 * (2+2+4) * sizeof(float), BufferUsage.VertexBuffer));
            _indices = new ushort[MaxQuads * 6];
            for (int i = 0, v = 0; i < MaxQuads; i++, v += 4)
            {
                _indices[i*6+0] = (ushort)(v+0);
                _indices[i*6+1] = (ushort)(v+1);
                _indices[i*6+2] = (ushort)(v+2);
                _indices[i*6+3] = (ushort)(v+0);
                _indices[i*6+4] = (ushort)(v+2);
                _indices[i*6+5] = (ushort)(v+3);
            }
            _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(_indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
            _gd.UpdateBuffer(_indexBuffer, 0, _indices);
        }

        public void Begin()
        {
            _batches.Clear();
            _currentBatch = null;
        }

        public void Draw(Texture texture, Rectangle destRect, Color color, float rotationRad, Vector2 origin)
        {
            float w = destRect.Width;
            float h = destRect.Height;
            float ox = origin.X;
            float oy = origin.Y;
        
            // Углы прямоугольника (локальные координаты относительно левого верхнего угла)
            Vector2[] corners =
            [
                new Vector2(0, 0),         // левый верх
                new Vector2(w, 0),         // правый верх
                new Vector2(w, h),         // правый низ
                new Vector2(0, h),         // левый низ
            ];

            // Поворот вокруг (ox, oy) и смещение на позицию прямоугольника
            float cos = (float)Math.Cos(rotationRad);
            float sin = (float)Math.Sin(rotationRad);
            Vector2[] transformed = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                float x = corners[i].X - ox;
                float y = corners[i].Y - oy;
                float xr = x * cos - y * sin + ox;
                float yr = x * sin + y * cos + oy;
                transformed[i] = new Vector2(destRect.X + xr, destRect.Y + yr);
            }
        
            // Инвертируем Y (переход в систему координат с началом в левом нижнем углу)
            float screenHeight = _gd.SwapchainFramebuffer.Height;
            for (int i = 0; i < 4; i++)
                transformed[i].Y = screenHeight - transformed[i].Y;
        
            // Преобразование в NDC
            float sx = 2.0f / _gd.SwapchainFramebuffer.Width;
            float sy = -2.0f / _gd.SwapchainFramebuffer.Height;
            float tx = -1.0f;
            float ty = 1.0f;
        
            // Порядок вершин для текстурных координат: левый низ, правый низ, правый верх, левый верх
            Vector2[] ordered =
            [
                transformed[3], // левый низ
                transformed[2], // правый низ
                transformed[1], // правый верх
                transformed[0], // левый верх
            ];
            float[] uvs = { 0, 1, 1, 1, 1, 0, 0, 0 }; // (u,v) для каждого из 4 углов в указанном порядке
        
            float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f, a = color.A / 255f;
        
            float[] vertices = new float[4 * (2 + 2 + 4)]; // 4 вершины * (x,y + u,v + r,g,b,a)
            for (int i = 0; i < 4; i++)
            {
                float x_ndc = ordered[i].X * sx + tx;
                float y_ndc = ordered[i].Y * sy + ty;
                float u = uvs[i * 2];
                float v = uvs[i * 2 + 1];
                int idx = i * 8;
                vertices[idx + 0] = x_ndc;
                vertices[idx + 1] = y_ndc;
                vertices[idx + 2] = u;
                vertices[idx + 3] = v;
                vertices[idx + 4] = r;
                vertices[idx + 5] = g;
                vertices[idx + 6] = b;
                vertices[idx + 7] = a;
            }
        
            // Добавляем в текущий батч (создаём новый при смене текстуры или переполнении)
            int verticesPerQuad = 4 * 8; // 32
            if (_currentBatch == null || _currentBatch.Value.Texture != texture ||
                (_currentBatch.Value.Vertices.Count + vertices.Length) / verticesPerQuad > MaxQuads)
            {
                _currentBatch = new Batch { Texture = texture, Vertices = new List<float>() };
                _batches.Add(_currentBatch.Value);
            }
        
            var batch = _currentBatch.Value;
            batch.Vertices.AddRange(vertices);
            _currentBatch = batch;
        }

        public void End()
        {
            foreach (var batch in _batches)
            {
                if (batch.Vertices.Count == 0) continue;

                // Используем командный буфер для обновления данных вершин
                _cl.UpdateBuffer(_vertexBuffer, 0, batch.Vertices.ToArray());

                if (!_resourceSets.TryGetValue(batch.Texture, out var resourceSet))
                {
                    var texView = _gd.ResourceFactory.CreateTextureView(batch.Texture);
                    var sampler = _gd.PointSampler;
                    resourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_resourceLayout, texView, sampler));
                    _resourceSets[batch.Texture] = resourceSet;
                }

                _cl.SetPipeline(_pipeline);
                _cl.SetVertexBuffer(0, _vertexBuffer);
                _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
                _cl.SetGraphicsResourceSet(0, resourceSet);

                int quadCount = batch.Vertices.Count / (4 * (2+2+4));
                int indexCount = quadCount * 6;
                _cl.DrawIndexed((uint)indexCount, 1, 0, 0, 0);
            }

            _batches.Clear();
            _currentBatch = null;
        }

        public void Dispose()
        {
            foreach (var rs in _resourceSets.Values)
                rs.Dispose();
            _resourceSets.Clear();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _pipeline.Dispose();
            _resourceLayout.Dispose();
        }
    }
}