export const DEPARTMENTS = ['Technical', 'Billing', 'General'] as const;
export type Department = (typeof DEPARTMENTS)[number];

export interface Agent {
  id: string;
  fullName: string;
  department: Department;
  active: boolean;
}
