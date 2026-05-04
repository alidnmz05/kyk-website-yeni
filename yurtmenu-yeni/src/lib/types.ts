export type MenuItem = {
  id: number;
  date: string;
  mealType: number;
  cityId: number;
  first?: string;
  firstCalories?: string;
  second?: string;
  secondCalories?: string;
  third?: string;
  thirdCalories?: string;
  fourth?: string;
  fourthCalories?: string;
  totalCalories?: number;
};

export type City = {
  id: number;
  name: string;
};
