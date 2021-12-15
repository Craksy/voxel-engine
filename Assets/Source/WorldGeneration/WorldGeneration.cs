using Source.Vox;
using UnityEngine;


namespace Vox.WorldGeneration{
    public class WorldGeneration
    {
        public static void GenerateFlatWorld(World world, Vector2Int from, Vector2Int to, WorldGenConfig config){
            var conf = (FlatWorldConfig)config;
            for(int x = from.x; x<to.x; x++){
                for(int z = from.y; z<to.y; z++){
                    world[x,conf.Height,z] = (byte)2;
                    for(int y = conf.Height-1; y>=0; y--){
                        world[x,y,z] = (byte)1;
                    }
                }
            }
        }

        public static void GenerateRandom(World world, Vector2Int coordinate, WorldGenConfig config) {
            var from = new Vector2Int(coordinate.x * GridManager.chunkWidth, coordinate.y * GridManager.chunkDepth);
            var to = new Vector2Int((coordinate.x + 1) * GridManager.chunkWidth,
                (coordinate.y + 1) * GridManager.chunkDepth);

            var r = new System.Random();
            for (var x = from.x; x < to.x; x++) {
                for (var z = from.y; z < to.y; z++) {
                    for (var y = 0; y < 200; y++) {
                        world[x, y, z] = (byte) r.Next(0, 3);
                    }
                }
            }
        }

        public static void GenerateSurface2(World world, Vector2Int coordinate, WorldGenConfig config){
            HillWorldConfig conf = (HillWorldConfig)config;
            var amplitude = conf.Amplitude;
            var persistence = conf.Persistence;
            var frequency = conf.Freqency;
            var octave = conf.Octaves;
            int seed;
            if(!int.TryParse(conf.Seed, out seed)){
                seed = conf.Seed.GetHashCode();
            }

            var nrange = 0f;
            var am = amplitude;
            var sqn = Mathf.Sqrt(0.5f);
            for(var i =0; i < octave; i++) {
                nrange += Mathf.Sqrt(0.5f) * am;
                am *= persistence;
            }

            nrange /= octave;
            float rangemod = 1 / nrange;
            Debug.Log("rangemod is " + rangemod);
            //var from = new Vector2Int(coordinate.x * GridManager.chunkWidth, coordinate.y * GridManager.chunkDepth);
            //var to = new Vector2Int((coordinate.x + 1) * GridManager.chunkWidth, (coordinate.y+1) * GridManager.chunkDepth);
            var from = coordinate * 128;
            var to = (coordinate + Vector2Int.one) * 128;
            Debug.Log($"generating chunk from {from} to {to}");
            for(var x = from.x; x<to.x;x++){
                for(var z = from.y; z<to.y;z++){
                    var val = Noise2D(x / 64f, z / 64f, 0, 0, frequency, amplitude, persistence, octave, 0);
                    var height = Mathf.Clamp((int)(val*rangemod*50), 0, 200);
                    Debug.Log($"val at {x}, {z}, {val} height {height}");
                    if(world.chunks.ContainsKey(GridManager.ChunkIdFromWorld(x,height,z))) {
                        world[x, height, z] = 2;
                    }

                    
                    val = Noise2D(x / 64f, z / 64f, 0, 0, frequency, amplitude, persistence, octave, seed+234);
                    var z2 = (int)(val*rangemod*10-30);
                    for(var i = height-1; i > 0; i--) {
                        world[x,i,z] = i<z2 ? (byte)3 :(byte)1;
                    }
                }
            }
        }

        public static void GenerateSurface(World world, Vector2Int from, Vector2Int to, float frequency, float amplitude, float persistence, int octave, int seed){
            Debug.Log($"Generate surface from block {from} to {to}");
            float nrange = 0f;
            float am = amplitude;
            for(int i =0; i < octave; i++) {
                nrange += Mathf.Sqrt(0.5f) * am;
                am *= persistence;
            }

            nrange /= octave;
            float rangemod = 1 / nrange;

            for(var x = from.x; x<to.x;x++){
                for(var y = from.y; y<to.y;y++){
                    float val = Noise2D(x / 64f, y / 64f, 0, 0, frequency, amplitude, persistence, octave, seed);
                    int z = Mathf.Clamp((int)(val*rangemod*80), 0, 120);
                    if(world.chunks.ContainsKey(GridManager.ChunkIdFromWorld(x,y,z))){
                        byte type;
                        type = 2;
                        world[x, z, y] = type;
                    }

                    val = Noise2D(x / 64f, y / 64f, 0, 0, frequency, amplitude, persistence, octave, seed+234);
                    int z2 = (int)(val *rangemod *10-30);
                    for(int i = z-1; i > 0; i--) {
                        world[x,i,y] = i<z2 ? (byte)3 :(byte)1;
                    }
                }
            }
        }

        public static float Noise2D(float x, float y, int offsetx, int offsety, float frequency, float amplitude, float persistence, int octave, int seed) {
            float noise = 0.0f;

            for (int i = 0; i < octave; ++i) {
                noise += Mathf.PerlinNoise(x*frequency + seed, y*frequency + seed) * amplitude;
                amplitude *= persistence;
                frequency *= 2.0f;
            }

            return noise / octave;
        }
    }

    [System.Serializable]
    public class WorldGenConfig{
        public string WorldName;
        public string WorldType;
    }

    [System.Serializable]
    public class HillWorldConfig : WorldGenConfig{
        [Range(0f, 3f)]
        public float Freqency = 0.3f; 

        [Range(0f, 10f)]
        public float Amplitude = 1.0f; 

        [Range(0f, 3f)]
        public float Persistence = 0.5f;

        [Range(0, 20)]
        public int Octaves = 3;

        public string Seed = "0";
    }

    [System.Serializable]
    public class FlatWorldConfig : WorldGenConfig{
        [Range(1, 256)]
        public int Height;
    }

}