using Infrastructure.Data.Context;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data.Seed.Auth
{
	public static class AppUserSeeder
	{
		public static void Seed(IServiceProvider serviceProvider)
		{
			using var scope = serviceProvider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<SiGAVContext>();
			
			if (!context.Users.Any())
			{
				var user = new AppUser
				{
					UserName = "admin",
					Identificacion = "0000000000",
					Nombre = "Admin",
					Apellido = "User",
					Sexo = Domain.Enums.SexoEnum.MASCULINO,
					Institucion = Domain.Enums.InstitucionEnum.MOPC,
					RangoId = 1, // Asumiendo que el rango con ID 1 existe
					DepartamentoId = 1, // Asumiendo que el departamento con ID 1 existe
					IsAdmin = true
				};
				
				var passwordHasher = new PasswordHasher<AppUser>();
				user.PasswordHash = passwordHasher.HashPassword(user, "Admin@123");
				context.Users.Add(user);
				context.SaveChanges();
			}
		}
	}
}
