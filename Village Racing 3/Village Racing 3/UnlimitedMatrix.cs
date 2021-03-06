﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Village_Racing_3
{
    /*
   This code is hereby released to the public domain.
   ~aaaaaa123456789, 2015-04-02
*/

    public class UnlimitedMatrix<T> : IEnumerable, IEnumerable<long[]>
    {
        private object[] quadrants;
        private byte[] depths;
        private bool is_value_type;

        public UnlimitedMatrix()
        {
            quadrants = new object[4];
            depths = new byte[4];
            is_value_type = typeof(T).IsValueType;
        }

        private bool is_default(T value)
        {
            if (is_value_type) return value.Equals(default(T));
            return (object)value == null;
        }

        private int quadrant_for(ref long x, ref long y)
        {
            int result = 0;
            if (x < 0)
            {
                x = ~x;
                result++;
            }
            if (y < 0)
            {
                y = ~y;
                result += 2;
            }
            return result;
        }

        private long max_index(byte depth)
        {
            if (depth >= 20) return 9223372036854775807;
            return (8L << (3 * depth)) - 1;
        }

        private byte depth_of(long index)
        {
            if (index < 0) return 255;
            byte current_depth = 0;
            while (index > 7)
            {
                current_depth++;
                index >>= 3;
            }
            return current_depth;
        }

        private object retrieve(object array, byte depth, long x, long y)
        {
            if (depth > 0)
            {
                array = this.retrieve(array, (byte)(depth - 1), x >> 3, y >> 3);
                x &= 7;
                y &= 7;
            }
            if (array == null) return null;
            return ((object[])array)[(x << 3) | y];
        }

        private void increase_depth(int quadrant, byte depth)
        {
            if (depth > 20) throw new System.ArgumentOutOfRangeException();
            if (this.quadrants[quadrant] == null)
            {
                this.quadrants[quadrant] = new T[64];
            }
            while (this.depths[quadrant] < depth)
            {
                object[] new_array = new object[65];
                new_array[0] = this.quadrants[quadrant];
                new_array[64] = 1;
                this.quadrants[quadrant] = new_array;
                this.depths[quadrant]++;
            }
        }

        private byte[] get_coords(long x, long y, byte depth)
        {
            byte[] coords = new byte[21];
            byte current_depth;
            for (current_depth = 0; current_depth <= depth; current_depth++)
            {
                coords[current_depth] = (byte)(((x & 7) << 3) + (y & 7));
                x >>= 3;
                y >>= 3;
            }
            return coords;
        }

        private bool is_empty(T[] array)
        {
            int p;
            for (p = 0; p < 64; p++) if (!is_default(array[p])) return false;
            return true;
        }

        private void reduce(object[] locations, byte[] coords, int quadrant)
        {
            int depth = 0;
            object[] p;
            while (true)
            {
                if (locations[depth + 1] == null)
                {
                    this.quadrants[quadrant] = null;
                    this.depths[quadrant] = 0;
                    return;
                }
                p = (object[])locations[depth + 1];
                p[coords[depth]] = null;
                p[64] = (int)p[64] - 1;
                if (((int)p[64]) != 0) break;
                depth++;
            }
            while (this.depths[quadrant] > 0)
            {
                p = (object[])this.quadrants[quadrant];
                if (((int)p[64]) != 1) return;
                if (p[0] == null) return;
                this.quadrants[quadrant] = p[0];
                this.depths[quadrant]--;
            }
        }

        private void set(int quadrant, long x, long y, T value)
        {
            byte current_depth = this.depths[quadrant];
            byte[] coords = get_coords(x, y, current_depth);
            object location = this.quadrants[quadrant];
            object[] p;
            for (; current_depth > 0; current_depth--)
            {
                p = (object[])location;
                if (p[coords[current_depth]] == null)
                {
                    if (current_depth == 1)
                        p[coords[current_depth]] = new T[64];
                    else
                    {
                        object[] vr = new object[65];
                        vr[64] = 0;
                        p[coords[current_depth]] = vr;
                    }
                    p[64] = (int)p[64] + 1;
                }
                location = p[coords[current_depth]];
            }
            ((T[])location)[coords[0]] = value;
        }

        public void unset(long x, long y)
        {
            if (is_default(this[x, y])) return;
            int quadrant = quadrant_for(ref x, ref y);
            byte current_depth = this.depths[quadrant];
            byte[] coords = get_coords(x, y, current_depth);
            object[] locations = new object[22];
            locations[current_depth] = this.quadrants[quadrant];
            while (current_depth > 0)
            {
                locations[current_depth - 1] = ((object[])locations[current_depth])[coords[current_depth]];
                current_depth--;
            }
            ((T[])locations[0])[coords[0]] = default(T);
            if (is_empty((T[])locations[0])) this.reduce(locations, coords, quadrant);
        }

        public T this[long x, long y]
        {
            get
            {
                int quad = quadrant_for(ref x, ref y);
                if (this.quadrants[quad] == null) return default(T);
                long rv = max_index(this.depths[quad]);
                if ((x > rv) || (y > rv)) return default(T);
                T[] array;
                if (this.depths[quad] > 0)
                {
                    array = (T[])this.retrieve(this.quadrants[quad], (byte)(this.depths[quad] - 1), x >> 3, y >> 3);
                    if (array == null) return default(T);
                    x &= 7;
                    y &= 7;
                }
                else
                {
                    array = (T[])this.quadrants[quad];
                }
                return array[(x << 3) | y];
            }
            set
            {
                if (is_default(value))
                {
                    this.unset(x, y);
                    return;
                }
                int quad = quadrant_for(ref x, ref y);
                byte depth = depth_of(x);
                byte t = depth_of(y);
                if (t > depth) depth = t;
                if ((this.quadrants[quad] == null) || (this.depths[quad] < depth))
                    this.increase_depth(quad, depth);
                this.set(quad, x, y, value);
            }
        }

        public T this[int x, int y]
        {
            set
            {
                this[(long)x, (long)y] = value;
            }
            get
            {
                return this[(long)x, (long)y];
            }
        }

        public void clear()
        {
            this.quadrants = new object[4];
            this.depths = new byte[4];
            System.GC.Collect();
        }

        private IEnumerable<long[]> enumerate_direct(T[] array, bool ascending, long x, long y, int quadrant)
        {
            x <<= 3;
            y <<= 3;
            int pos, index;
            long effectiveX, effectiveY;
            for (pos = 0; pos < 64; pos++)
            {
                index = (pos & 1) | ((pos >> 1) & 2) | ((pos >> 2) & 4) | ((((pos >> 1) & 1) | ((pos >> 2) & 2) | ((pos >> 3) & 4)) << 3);
                if (!ascending) index ^= 63;
                if (is_default(array[index])) continue;
                effectiveX = x + (index >> 3);
                effectiveY = y + (index & 7);
                if ((quadrant & 1) == 1) effectiveX = ~effectiveX;
                if ((quadrant & 2) == 2) effectiveY = ~effectiveY;
                yield return new long[] { effectiveX, effectiveY };
            }
        }

        private IEnumerable<long[]> enumerate_indirect(object[] array, bool ascending, byte depth, long x, long y, int quadrant)
        {
            depth--;
            x <<= 3;
            y <<= 3;
            int pos, index;
            for (pos = 0; pos < 64; pos++)
            {
                index = (pos & 1) | ((pos >> 1) & 2) | ((pos >> 2) & 4) | ((((pos >> 1) & 1) | ((pos >> 2) & 2) | ((pos >> 3) & 4)) << 3);
                if (!ascending) index ^= 63;
                if (array[index] == null) continue;
                if (depth == 0)
                    foreach (long[] current in enumerate_direct((T[])array[index], ascending, x + (index >> 3), y + (index & 7), quadrant))
                        yield return current;
                else
                    foreach (long[] current in enumerate_indirect(
                      (object[])array[index],
                      ascending,
                      depth,
                      x + (index >> 3),
                      y + (index & 7),
                      quadrant
                    )) yield return current;
            }
        }

        private IEnumerable<long[]> enumerate_quadrant(int quadrant, bool ascending)
        {
            if (quadrants[quadrant] == null) yield break;
            if (depths[quadrant] == 0)
            {
                foreach (long[] current in enumerate_direct(
                  (T[])quadrants[quadrant], ascending, 0, 0, quadrant
                )) yield return current;
                yield break;
            }
            foreach (long[] current in enumerate_indirect(
              (object[])quadrants[quadrant], ascending, depths[quadrant], 0, 0, quadrant
            )) yield return current;
        }

        public IEnumerator<long[]> GetEnumerator()
        {
            foreach (long[] current in enumerate_quadrant(1, false)) yield return current;
            foreach (long[] current in enumerate_quadrant(0, true)) yield return current;
            foreach (long[] current in enumerate_quadrant(3, false)) yield return current;
            foreach (long[] current in enumerate_quadrant(2, true)) yield return current;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
