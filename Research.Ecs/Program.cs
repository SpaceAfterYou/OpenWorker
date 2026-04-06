// See https://aka.ms/new-console-template for more information

using Arch.Core;

Console.WriteLine("Hello, World!");

var world = World.Create();
var player = world.Create();
var enemy = world.Create();
var ally = world.Create();

world.Destroy(enemy);

// enemy = world.Create();

var x = 1;
