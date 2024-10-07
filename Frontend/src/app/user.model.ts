export interface User {
  id: string; // Match the type with the C# class (int -> number)
  Email: string;
  PasswordHash: string; // Consider handling password as a hashed string on the backend
  DateOfBirth: Date; // Keep Date for date representation
  IdentityCardNumber: string; // Ensure consistency with IdentityCardNumber
  profilePicture: File | null; // To handle file uploads
}
