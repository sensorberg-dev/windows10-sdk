﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace SensorbergShowcase.Models
{
    public class PhotoCameraLuminanceSource : LuminanceSource
    {
        public byte[] PreviewBufferY { get; set; }

        public PhotoCameraLuminanceSource(int width, int height)
            : base(width, height)
        {
            PreviewBufferY = new byte[width*height];
        }


        public override byte[] Matrix
        {
            get { return (byte[]) (Array) PreviewBufferY; }
        }

        public override byte[] getRow(int y, byte[] row)
        {
            if (row == null || row.Length < Width)
            {
                row = new byte[Width];
            }

            for (int i = 0; i < Height; i++)
                row[i] = (byte) PreviewBufferY[i*Width + y];

            return row;
        }
    }
}
