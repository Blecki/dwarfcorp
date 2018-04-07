#include "stdafx.h"
#include "Level2.h"
#include <memory>

using namespace SparseVoxelTreePerformanceTest;

int SparseVoxelTreePerformanceTest::EncodeIndex(int x, int y, int z)
{
	return (z << 2) + (y << 1) + x;
}

Level4::Level4(unsigned char x, unsigned char y, unsigned char z, int voxel)
	: X(x), Y(y), Z(z), Voxel(voxel), Children(nullptr)
{}

Level4::~Level4()
{
	if (Children != nullptr)
		delete [] Children;
}

void Level4::Subdivide()
{
	Children = new unsigned int[4 * 4 * 4];
	memset(Children, Voxel, 4 * 4 * 4);
}

int Level4::GetVoxel(unsigned char x, unsigned char y, unsigned char z)
{
	if (Children == nullptr)
		return Voxel;
	return Children[EncodeIndex(x, y, z)];
}

void Level4::SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel)
{
	if (Children == nullptr)
	{
		if (Voxel == voxel)
			return;
		Subdivide();
	}
	Children[EncodeIndex(x, y, z)] = voxel;
}

int Level4::EncodeIndex(unsigned char x, unsigned char y, unsigned char z)
{
	int _x = (x - X);// &0x3;
	int _y = (y - Y);// &0x3;
	int _z = (z - Z);// &0x3;

	return (_z << 4) + (_y << 2) + _x;
}


Level8::Level8(unsigned char x, unsigned char y, unsigned char z, int voxel)
	: X(x), Y(y), Z(z), Voxel(voxel), Children(nullptr)
{}

Level8::~Level8()
{
	if (Children != nullptr)
		delete[] Children;
}

void Level8::Subdivide()
{
	Children = new Level4[8]
	{
		Level4(X,     Y,     Z,     Voxel),
		Level4(X + 4, Y,     Z,     Voxel),
		Level4(X,     Y + 4, Z,     Voxel),
		Level4(X + 4, Y + 4, Z,     Voxel),
		Level4(X,     Y,     Z + 4, Voxel),
		Level4(X + 4, Y,     Z + 4, Voxel),
		Level4(X,     Y + 4, Z + 4, Voxel),
		Level4(X + 4, Y + 4, Z + 4, Voxel)
	};
}

int Level8::GetVoxel(unsigned char x, unsigned char y, unsigned char z)
{
	if (Children == nullptr)
		return Voxel;
	return Children[EncodeIndex(x, y, z)].GetVoxel(x, y, z);
}

void Level8::SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel)
{
	if (Children == nullptr)
	{
		if (Voxel == voxel)
			return;
		Subdivide();
	}
	Children[EncodeIndex(x, y, z)].SetVoxel(x, y, z, voxel);
}

int Level8::EncodeIndex(unsigned char x, unsigned char y, unsigned char z)
{
	int _x = ((x - X) >> 2);// &0x1;
	int _y = ((y - Y) >> 2);// &0x1;
	int _z = ((z - Z) >> 2);// &0x1;

	return SparseVoxelTreePerformanceTest::EncodeIndex(_x, _y, _z);
}


Level16::Level16(unsigned char x, unsigned char y, unsigned char z, int voxel)
	: X(x), Y(y), Z(z), Voxel(voxel), Children(nullptr)
{}

Level16::~Level16()
{
	if (Children != nullptr)
		delete[] Children;
}

void Level16::Subdivide()
{
	Children = new Level8[8]
	{
		Level8(X,     Y,     Z,     Voxel),
		Level8(X + 8, Y,     Z,     Voxel),
		Level8(X,     Y + 8, Z,     Voxel),
		Level8(X + 8, Y + 8, Z,     Voxel),
		Level8(X,     Y,     Z + 8, Voxel),
		Level8(X + 8, Y,     Z + 8, Voxel),
		Level8(X,     Y + 8, Z + 8, Voxel),
		Level8(X + 8, Y + 8, Z + 8, Voxel)
	};
}

int Level16::GetVoxel(unsigned char x, unsigned char y, unsigned char z)
{
	if (Children == nullptr)
		return Voxel;
	return Children[EncodeIndex(x, y, z)].GetVoxel(x, y, z);
}

void Level16::SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel)
{
	if (Children == nullptr)
	{
		if (Voxel == voxel)
			return;
		Subdivide();
	}
	Children[EncodeIndex(x, y, z)].SetVoxel(x, y, z, voxel);
}

int Level16::EncodeIndex(unsigned char x, unsigned char y, unsigned char z)
{
	int _x = ((x - X) >> 3);// &0x1;
	int _y = ((y - Y) >> 3);// &0x1;
	int _z = ((z - Z) >> 3);// &0x1;

	return SparseVoxelTreePerformanceTest::EncodeIndex(_x, _y, _z);
}


Chunk::Chunk(int voxel)
	: Voxel(voxel), Children(nullptr)
{}

Chunk::~Chunk()
{
	if (Children != nullptr)
		delete[] Children;
}

void Chunk::Subdivide()
{
	Children = new Level16[4]
	{
		Level16(0, 0,  0, Voxel),
		Level16(0, 16, 0, Voxel),
		Level16(0, 32, 0, Voxel),
		Level16(0, 48, 0, Voxel),
	};
}

int Chunk::GetVoxel(unsigned char x, unsigned char y, unsigned char z)
{
	if (Children == nullptr)
		return Voxel;
	return Children[EncodeIndex(x, y, z)].GetVoxel(x, y, z);
}

void Chunk::SetVoxel(unsigned char x, unsigned char y, unsigned char z, int voxel)
{
	if (Children == nullptr)
	{
		if (Voxel == voxel)
			return;
		Subdivide();
	}
	Children[EncodeIndex(x, y, z)].SetVoxel(x, y, z, voxel);
}

int Chunk::EncodeIndex(unsigned char x, unsigned char y, unsigned char z)
{
	return (y >> 4);// &0x3;
}

int Level4::GetMemoryUsage()
{
	return sizeof(Level4) + (Children == nullptr ? 0 : (sizeof(unsigned int) * 4 * 4 * 4));
}

int Level8::GetMemoryUsage()
{
	int bytesUsed = sizeof(Level8);
	if (Children != nullptr)
		for (int i = 0; i < 8; ++i)
			bytesUsed += Children[i].GetMemoryUsage();
	return bytesUsed;
}

int Level16::GetMemoryUsage()
{
	int bytesUsed = sizeof(Level16);
	if (Children != nullptr)
		for (int i = 0; i < 8; ++i)
			bytesUsed += Children[i].GetMemoryUsage();
	return bytesUsed;
}

int Chunk::GetMemoryUsage()
{
	int bytesUsed = 8;// sizeof(Chunk);
	if (Children != nullptr)
		for (int i = 0; i < 4; ++i)
			bytesUsed += Children[i].GetMemoryUsage();
	return bytesUsed;
}

bool Level4::Compact()
{
	if (Children == nullptr)
		return true;
	unsigned int v = Children[0];
	for (int i = 1; i < (4 * 4 * 4); ++i)
		if (Children[i] != v) return false;
	Voxel = v;
	delete[] Children;
	Children = nullptr;
	return true;
}

bool Level8::Compact()
{
	if (Children == nullptr)
		return true;
	unsigned int v = Children[0].Voxel;
	bool childrenCompact = true;
	for (int i = 0; i < 8; ++i)
	{
		childrenCompact &= Children[i].Compact();
		childrenCompact &= (Children[i].Voxel == v);
	}
	if (childrenCompact)
	{
		delete[] Children;
		Children = nullptr;
		Voxel = v;
		return true;
	}
	return false;
}

bool Level16::Compact()
{
	if (Children == nullptr)
		return true;
	unsigned int v = Children[0].Voxel;
	bool childrenCompact = true;
	for (int i = 0; i < 8; ++i)
	{
		childrenCompact &= Children[i].Compact();
		childrenCompact &= (Children[i].Voxel == v);
	}
	if (childrenCompact)
	{
		delete[] Children;
		Children = nullptr;
		Voxel = v;
		return true;
	}
	return false;
}

void Chunk::Compact()
{
	if (Children == nullptr)
		return;
	unsigned int v = Children[0].Voxel;
	bool childrenCompact = true;
	for (int i = 0; i < 4; ++i)
	{
		childrenCompact &= Children[i].Compact();
		childrenCompact &= (Children[i].Voxel == v);
	}
	if (childrenCompact)
	{
		delete[] Children;
		Children = nullptr;
		Voxel = v;
	}
}