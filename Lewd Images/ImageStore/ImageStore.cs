﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Lewd_Images
{
    public abstract class ImageStore
    {
        /// <summary>
        /// List of image urls
        /// </summary>
        protected readonly List<string> list = new List<string>();

        private int m_index = -1;
        /// <summary>
        /// Index of the list, -1 if no images exist
        /// </summary>
        public int Index {
            get => m_index;
            protected set {
                if(value >= list.Count)
                    throw new IndexOutOfRangeException();
                m_index = value;
            }
        }

        /// <summary>
        /// Gets a new url to the ImageStore internal array
        /// </summary>
        /// <returns>Image url</returns>
        protected abstract string GetNew();

        /// <summary>
        /// Adds a custom link to internal list
        /// </summary>
        /// <param name="url">Custom link</param>
        public void AddLink(string url)
        {
            list.Add(url);
            Index++;
        }

        /// <summary>
        /// Moves the <see cref="Index"/> forward and calls <see cref="AppendNew"/> if no urls are available
        /// </summary>
        /// <param name="count"></param>
        public void Forward(int count = 1)
        {
            while((Index + count) > list.Count-1)
            {
                list.Add(GetNew());
            }
            Index += count;
        }
        /// <summary>
        /// Moves the <see cref="Index"/> back
        /// </summary>
        /// <param name="count"></param>
        public void Back(int count = 1)
        {
            Index -= count;
            if (Index < 0) Index = 0;
        }
        /// <summary>
        /// Goes to the last image available
        /// </summary>
        public void GotoLast()
        {
            while(!IsLast)
            {
                Forward();
            }
        }

        /// <summary>
        /// Removes all urls stored and sets <see cref="Index"/> to -1
        /// </summary>
        public void Reset()
        {
            list.Clear();
            Index = -1;
        }

        /// <summary>
        /// Returns the cached image, If internal image cache is null, download the image and set the cache
        /// </summary>
        /// <returns>Current image</returns>
        public Bitmap GetImage()
        {
            return new DownloadImageTask(null, null).Execute(GetLink()).GetResult();
        }

        public void SetImage(ImageView imageView, Action post)
        {
            new DownloadImageTask(imageView, post).Execute(GetLink());
        }

        private class DownloadImageTask : AsyncTask<string, string, Bitmap> {
            readonly ImageView bmImage;
            readonly bool animate;
            readonly Action post;

            public DownloadImageTask(ImageView bmImage, Action post, bool animate = true)
            {
                this.bmImage = bmImage;
                this.animate = animate;
                this.post = post;
            }

            protected override Bitmap RunInBackground(params string[] urls)
            {
                using (Stream stream = new Java.Net.URL(urls[0]).OpenStream())
                {
                    return BitmapFactory.DecodeStream(stream);
                }
            }
            protected override void OnPostExecute(Bitmap result)
            {
                bmImage?.SetImageBitmap(result);
                post?.Invoke();
            }
        }

        /// <summary>
        /// Returns link to the current image
        /// </summary>
        /// <returns>Link to current image</returns>
        public string GetLink()
        {
            if (Index < 0)
                throw new ImageStoreEmptyException();
            return list[Index];
        }

        /// <summary>
        /// If <see cref="Index"/> points to the last image in the list
        /// </summary>
        public bool IsLast => Index == list.Count - 1;
        /// <summary>
        /// If <see cref="Index"/> points to the first image in the list
        /// </summary>
        public bool IsFirst => Index <= 0;
    }

    public class ImageStoreEmptyException : Exception
    {
        public override string Message => "Tried to get image while ImageStore contained no images";
    }
}
