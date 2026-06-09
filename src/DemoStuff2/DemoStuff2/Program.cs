

using DemoStuff2;

var cat1 = new Pet
{
    Name = "Bailey"
};

var cat2 = new Pet
{
    Name = "Spike",
    Breed = "Alley Cat"
};

var cat3 = cat1;


var myName = "Jeffry";

var newName = myName.ToUpper();
var updatedSpike = cat2 with { Breed = "Siamese" };



Console.Write(myName + " " + newName); // Jeffry JEFFRY

Console.WriteLine("The Cats are Equal: " + (cat1 == cat2).ToString());

Console.WriteLine(cat1.ToString());
