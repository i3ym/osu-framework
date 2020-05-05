// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using osuTK;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public abstract class TextureGL : IDisposable
    {
        public TextureGL(WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;
        }

        #region Disposal

        ~TextureGL()
        {
            Dispose(false);
        }

        internal virtual bool IsQueuedForUpload { get; set; }

        /// <summary>
        /// By default, texture uploads are queued for upload at the beginning of each frame, allowing loading them ahead of time.
        /// When this is true, this will be bypassed and textures will only be uploaded on use. Should be set for every-frame texture uploads
        /// to avoid overloading the global queue.
        /// </summary>
        public bool BypassTextureUploadQueueing;

        /// <summary>
        /// Whether this <see cref="TextureGL"/> can used for drawing.
        /// </summary>
        public bool Available { get; private set; } = true;

        private bool isDisposed;

        protected virtual void Dispose(bool isDisposing) => GLWrapper.ScheduleDisposal(() => Available = false);

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public abstract TextureGL Native { get; }

        public abstract bool Loaded { get; }

        public Opacity Opacity { get; protected set; } = Opacity.Mixed;

        public abstract int TextureId { get; }

        public abstract int Height { get; set; }

        public abstract int Width { get; set; }

        public abstract RectangleI Bounds { get; }

        public Vector2 Size => new Vector2(Width, Height);

        public readonly WrapMode WrapModeS;

        public readonly WrapMode WrapModeT;

        public abstract RectangleF GetTextureRect(RectangleF? textureRect);

        /// <summary>
        /// Draws a triangle to the screen.
        /// </summary>
        /// <param name="vertexTriangle">The triangle to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <paramref name="textureRect"/> should be inflated.</param>
        /// <param name="textureCoords">The texture coordinates of the triangle's vertices (translated from the corresponding quad's rectangle).</param>
        internal abstract void DrawTriangle(Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                            Vector2? inflationPercentage = null, RectangleF? textureCoords = null);

        /// <summary>
        /// Draws a quad to the screen.
        /// </summary>
        /// <param name="vertexQuad">The quad to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <paramref name="textureRect"/> should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the <paramref name="textureRect"/> should be blended.</param>
        /// <param name="textureCoords">The texture coordinates of the quad's vertices.</param>
        internal abstract void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                        Vector2? blendRangeOverride = null, RectangleF? textureCoords = null);

        /// <summary>
        /// Bind as active texture.
        /// </summary>
        /// <param name="unit">The texture unit to bind to. Defaults to Texture0.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>True if bind was successful.</returns>
        public abstract bool Bind(TextureUnit unit = TextureUnit.Texture0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null);

        /// <summary>
        /// Uploads pending texture data to the GPU if it exists.
        /// </summary>
        /// <returns>Whether pending data existed and an upload has been performed.</returns>
        internal abstract bool Upload();

        /// <summary>
        /// Flush any unprocessed uploads without actually uploading.
        /// </summary>
        internal abstract void FlushUploads();

        public abstract void SetData(ITextureUpload upload, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null, Opacity? opacity = null);

        protected static Opacity ComputeOpacity(ITextureUpload upload)
        {
            if (upload is VideoTextureUpload)
                return Opacity.Opaque;

            if (upload.Data.Length == 0)
                return Opacity.Transparent;

            bool isTransparent = true;
            bool isOpaque = true;
            for (int i = 0; i < upload.Data.Length; ++i)
            {
                isTransparent &= upload.Data[i].A == 0;
                isOpaque &= upload.Data[i].A == 255;

                if (!isTransparent && !isOpaque)
                    return Opacity.Mixed;
            }

            if (isTransparent)
                return Opacity.Transparent;
            return Opacity.Opaque;
        }

        protected Opacity UpdateOpacity(ITextureUpload upload, Opacity? opacity)
        {
            // Compute opacity if it doesn't exist
            Opacity localOpacity = opacity ?? ComputeOpacity(upload);
            if (upload.Bounds == Bounds && upload.Level == 0)
                Opacity = localOpacity;
            else if (localOpacity != Opacity)
                Opacity = Opacity.Mixed;
            return localOpacity;
        }
    }

    public enum WrapMode
    {
        None = 0,
        ClampToEdge = 1,
        ClampToBorder = 2,
        Repeat = 3,
    }

    public enum Opacity
    {
        Opaque,
        Mixed,
        Transparent,
    }
}
