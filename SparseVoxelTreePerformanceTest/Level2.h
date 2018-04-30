#pragma once

namespace SparseVoxelTreePerformanceTest
{
	inline int EncodeIndex(int x, int y, int z);
	
	public class Level4
	{
		unsigned char X;
		unsigned char Y;
		unsigned char Z;
		unsigned int* Children;
		void Subdivide();
		int EncodeIndex(unsigned char x, unsigned char y, unsigned char z);
	public:
		unsigned int Voxel;
		Level4(unsigned char x, unsigned char y, unsigned char z, int voxel);
		~Level4();
		int GetVoxel(unsigned char x, unsigned char y, unsigned char z);
		void SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel);
		int GetMemoryUsage();
		bool Compact();
	};

	public class Level8
	{
		unsigned char X;
		unsigned char Y;
		unsigned char Z;
		Level4* Children;	
		void Subdivide();
		int EncodeIndex(unsigned char x, unsigned char y, unsigned char z);
	public:
		unsigned int Voxel;
		Level8(unsigned char x, unsigned char y, unsigned char z, int voxel);
		~Level8();
		int GetVoxel(unsigned char x, unsigned char y, unsigned char z);
		void SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel);
		int GetMemoryUsage();
		bool Compact();
	};

	public class Level16
	{
		unsigned char X;
		unsigned char Y;
		unsigned char Z;
		Level8* Children;
		void Subdivide();
		int EncodeIndex(unsigned char x, unsigned char y, unsigned char z);
	public:
		unsigned int Voxel;
		Level16(unsigned char x, unsigned char y, unsigned char z, int voxel);
		~Level16();
		int GetVoxel(unsigned char x, unsigned char y, unsigned char z);
		void SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel);
		int GetMemoryUsage();
		bool Compact();
	};

	public ref class Chunk
	{
		unsigned int Voxel;
		Level16* Children;
		void Subdivide();
		int EncodeIndex(unsigned char x, unsigned char y, unsigned char z);
	public:
		Chunk(int voxel);
		~Chunk();
		int GetVoxel(unsigned char x, unsigned char y, unsigned char z);
		void SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel);
		int GetMemoryUsage();
		void Compact();
	};
}
